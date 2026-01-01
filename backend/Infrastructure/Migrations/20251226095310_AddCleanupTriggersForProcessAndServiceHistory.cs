using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCleanupTriggersForProcessAndServiceHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ========================================
            // 1. ProcessSnapshots Cleanup Trigger
            // ========================================
            // Limit to 100 snapshots per server (oldest deleted first)
            // Child ProcessInfo records cascade delete automatically
            
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION limit_process_snapshots()
                RETURNS TRIGGER AS $$
                DECLARE
                    current_count INTEGER;
                    max_rows INTEGER := 100;  -- Keep last 100 snapshots per server
                    rows_to_delete INTEGER;
                BEGIN
                    -- Count current snapshots for this specific server
                    SELECT COUNT(*) INTO current_count
                    FROM ""ProcessSnapshots""
                    WHERE ""MonitoredServerId"" = NEW.""MonitoredServerId"";
                    
                    -- If count exceeds limit, delete oldest snapshots
                    IF current_count > max_rows THEN
                        rows_to_delete := current_count - max_rows;
                        
                        DELETE FROM ""ProcessSnapshots""
                        WHERE ""Id"" IN (
                            SELECT ""Id""
                            FROM ""ProcessSnapshots""
                            WHERE ""MonitoredServerId"" = NEW.""MonitoredServerId""
                            ORDER BY ""TimestampUtc"" ASC
                            LIMIT rows_to_delete
                        );
                        
                        -- Log deletion (visible in PostgreSQL logs)
                        RAISE LOG 'Deleted % old process snapshots for server %', rows_to_delete, NEW.""MonitoredServerId"";
                    END IF;
                    
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
            ");

            // Attach trigger to ProcessSnapshots table
            migrationBuilder.Sql(@"
                CREATE TRIGGER trigger_limit_process_snapshots
                AFTER INSERT ON ""ProcessSnapshots""
                FOR EACH ROW
                EXECUTE FUNCTION limit_process_snapshots();
            ");

            // ========================================
            // 2. ServiceStatusHistory Cleanup Trigger
            // ========================================
            // Limit to 1000 history entries per service (oldest deleted first)
            
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION limit_service_status_history()
                RETURNS TRIGGER AS $$
                DECLARE
                    current_count INTEGER;
                    max_rows INTEGER := 1000;  -- Keep last 1000 status changes per service
                    rows_to_delete INTEGER;
                BEGIN
                    -- Count current history entries for this specific service
                    SELECT COUNT(*) INTO current_count
                    FROM ""ServiceStatusHistories""
                    WHERE ""ServiceId"" = NEW.""ServiceId"";
                    
                    -- If count exceeds limit, delete oldest entries
                    IF current_count > max_rows THEN
                        rows_to_delete := current_count - max_rows;
                        
                        DELETE FROM ""ServiceStatusHistories""
                        WHERE ""Id"" IN (
                            SELECT ""Id""
                            FROM ""ServiceStatusHistories""
                            WHERE ""ServiceId"" = NEW.""ServiceId""
                            ORDER BY ""TimestampUtc"" ASC
                            LIMIT rows_to_delete
                        );
                        
                        -- Log deletion (visible in PostgreSQL logs)
                        RAISE LOG 'Deleted % old service status entries for service %', rows_to_delete, NEW.""ServiceId"";
                    END IF;
                    
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
            ");

            // Attach trigger to ServiceStatusHistories table
            migrationBuilder.Sql(@"
                CREATE TRIGGER trigger_limit_service_status_history
                AFTER INSERT ON ""ServiceStatusHistories""
                FOR EACH ROW
                EXECUTE FUNCTION limit_service_status_history();
            ");

            // ========================================
            // 3. Alerts Cleanup Trigger
            // ========================================
            // Delete acknowledged alerts older than 90 days
            // Keeps unacknowledged alerts indefinitely (they need attention)
            
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION cleanup_old_alerts()
                RETURNS TRIGGER AS $$
                DECLARE
                    deleted_count INTEGER;
                    cutoff_date TIMESTAMP;
                BEGIN
                    -- Calculate cutoff date (90 days ago)
                    cutoff_date := NOW() - INTERVAL '90 days';
                    
                    -- Delete old acknowledged alerts for this server
                    DELETE FROM ""Alerts""
                    WHERE ""MonitoredServerId"" = NEW.""MonitoredServerId""
                      AND ""IsAcknowledged"" = TRUE
                      AND ""AcknowledgedAtUtc"" < cutoff_date;
                    
                    GET DIAGNOSTICS deleted_count = ROW_COUNT;
                    
                    -- Log deletion if any alerts were removed
                    IF deleted_count > 0 THEN
                        RAISE LOG 'Deleted % old acknowledged alerts for server %', deleted_count, NEW.""MonitoredServerId"";
                    END IF;
                    
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
            ");

            // Attach trigger to Alerts table (runs after insert to cleanup old ones)
            migrationBuilder.Sql(@"
                CREATE TRIGGER trigger_cleanup_old_alerts
                AFTER INSERT ON ""Alerts""
                FOR EACH ROW
                EXECUTE FUNCTION cleanup_old_alerts();
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop triggers first, then functions
            
            // ProcessSnapshots cleanup
            migrationBuilder.Sql(@"
                DROP TRIGGER IF EXISTS trigger_limit_process_snapshots ON ""ProcessSnapshots"";
            ");
            
            migrationBuilder.Sql(@"
                DROP FUNCTION IF EXISTS limit_process_snapshots();
            ");

            // ServiceStatusHistory cleanup
            migrationBuilder.Sql(@"
                DROP TRIGGER IF EXISTS trigger_limit_service_status_history ON ""ServiceStatusHistories"";
            ");
            
            migrationBuilder.Sql(@"
                DROP FUNCTION IF EXISTS limit_service_status_history();
            ");

            // Alerts cleanup
            migrationBuilder.Sql(@"
                DROP TRIGGER IF EXISTS trigger_cleanup_old_alerts ON ""Alerts"";
            ");
            
            migrationBuilder.Sql(@"
                DROP FUNCTION IF EXISTS cleanup_old_alerts();
            ");
        }
    }
}
