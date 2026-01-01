# SSL Certificate Files

This folder contains the SSL certificate files used for HTTPS/WSS connections.

## Files
- `cert.pem` - Public SSL certificate
- `key.pem` - Private key (?? Keep secure! Never commit to public repositories)
- `certificate.pfx` - (Optional) Windows/IIS compatible certificate format

## Generating New Self-Signed Certificates

If you need to regenerate the certificates, use OpenSSL:

```bash
# Generate private key
openssl genrsa -out key.pem 2048

# Generate certificate signing request
openssl req -new -key key.pem -out cert.csr

# Generate self-signed certificate (valid for 365 days)
openssl x509 -req -days 365 -in cert.csr -signkey key.pem -out cert.pem

# (Optional) Convert to PFX format
openssl pkcs12 -export -out certificate.pfx -inkey key.pem -in cert.pem
```

## Security Best Practices

### ?? DO NOT
- Commit `key.pem` to version control
- Share the private key with unauthorized users
- Use self-signed certificates in production
- Reuse certificates across different environments

### ? DO
- Use `.gitignore` to exclude `*.pem` and `*.pfx` files
- Set restrictive file permissions (Linux: `chmod 600 key.pem`)
- Rotate certificates regularly
- Use environment-specific certificates (dev/staging/prod)
- For production, use certificates from trusted CAs (Let's Encrypt, DigiCert, etc.)

## Certificate Information

To view certificate details:
```bash
openssl x509 -in cert.pem -text -noout
```

## Troubleshooting

### "Certificate not found" error
- Verify files exist in this directory
- Check file permissions
- Ensure paths in `appsettings.json` are correct

### "Certificate validation failed" in agent
- For development: Disable SSL verification in agent
- For production: Ensure agent trusts the CA that signed the certificate

### Browser shows "Not Secure" warning
- Normal for self-signed certificates
- Click "Advanced" ? "Proceed" to accept
- For production, use CA-signed certificates

## For Production Use

Replace self-signed certificates with CA-signed certificates:

1. **Let's Encrypt (Free)**:
   ```bash
   certbot certonly --standalone -d yourdomain.com
   ```

2. **Commercial CA**: Purchase from DigiCert, GoDaddy, etc.

3. **Update configuration**: Point to new certificate files in `appsettings.json`

---

**Last Updated**: Generated during WSS configuration setup
**Certificate Type**: Self-signed (Development Only)
