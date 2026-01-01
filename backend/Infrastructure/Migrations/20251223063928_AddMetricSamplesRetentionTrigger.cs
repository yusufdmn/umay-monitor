using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMetricSamplesRetentionTrigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create trigger function to limit MetricSamples to 1000 rows per server
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION limit_metric_samples()
                RETURNS TRIGGER AS $$
                DECLARE
                    current_count INTEGER;
                    max_rows INTEGER := 1000;  -- Keep last 1000 rows per server
                    rows_to_delete INTEGER;
                BEGIN
                    -- Count current rows for this specific server
                    SELECT COUNT(*) INTO current_count
                    FROM ""MetricSamples""
                    WHERE ""MonitoredServerId"" = NEW.""MonitoredServerId"";
                    
                    -- If count exceeds limit, delete oldest rows
                    IF current_count > max_rows THEN
                        rows_to_delete := current_count - max_rows;
                        
                        DELETE FROM ""MetricSamples""
                        WHERE ""Id"" IN (
                            SELECT ""Id""
                            FROM ""MetricSamples""
                            WHERE ""MonitoredServerId"" = NEW.""MonitoredServerId""
                            ORDER BY ""TimestampUtc"" ASC
                            LIMIT rows_to_delete
                        );
                        
                        -- Log deletion (visible in PostgreSQL logs with log_min_messages = LOG)
                        RAISE LOG 'Deleted % old metric samples for server %', rows_to_delete, NEW.""MonitoredServerId"";
                    END IF;
                    
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
            ");

            // Attach trigger to MetricSamples table (fires after each insert)
            migrationBuilder.Sql(@"
                CREATE TRIGGER trigger_limit_metric_samples
                AFTER INSERT ON ""MetricSamples""
                FOR EACH ROW
                EXECUTE FUNCTION limit_metric_samples();
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop trigger first, then function
            migrationBuilder.Sql(@"
                DROP TRIGGER IF EXISTS trigger_limit_metric_samples ON ""MetricSamples"";
            ");
            
            migrationBuilder.Sql(@"
                DROP FUNCTION IF EXISTS limit_metric_samples();
            ");
        }
    }
}
