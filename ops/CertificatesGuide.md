# Certificates Guide (OPC UA)

PlcLab OPC UA client uses X.509 certificates with explicit trust-list handling.

## The secure connection workflow:

Enable TLS toggle → first connection attempt fails (untrusted server cert).
SDK saves the server cert to certs.
Go to Certificates page in the web app → the server cert appears in the Rejected list.
Click Trust → cert moves to certs.
Reconnect → secure channel succeeds.

## Security baseline

- `AutoAcceptUntrustedCertificates = false` is enforced in the client session adapter.
- Client PKI directories:
	- `pki/own` (client cert + private key)
	- `pki/trusted` (trusted remote certs)
	- `pki/rejected` (new/untrusted certs)

## Generate or rotate client certificate

Use the script:

```powershell
pwsh ./scripts/Rotate-OpcUaClientCertificate.ps1 -PkiRoot ./pki -Force
```

Outputs in `pki/own`:
- `client.current.cer`
- `client.current.pfx`
- `client.current.password.txt`

Previous `client.current.*` files are archived under `pki/own/archive`.

## Trust list management in UI

Open the **Certificates** tab in the app.

Actions:
- **Trust**: move a certificate from rejected to trusted.
- **Reject**: move a certificate from trusted to rejected.
- **Delete**: permanently remove a rejected certificate file.

API endpoints used by the UI:
- `GET /api/certificates`
- `POST /api/certificates/promote`
- `POST /api/certificates/reject`
- `DELETE /api/certificates/rejected/{fileName}`

## Testing the UI locally (without Docker)

`appsettings.Development.json` sets `OpcUa:PkiRootPath` to `../../pki`, which resolves to the repo-root
`pki/` folder when the app runs from `src/PlcLab.Web`.

### Quick smoke test in three steps

1. **Generate the client certificate** (run once):
   ```powershell
   pwsh ./scripts/Rotate-OpcUaClientCertificate.ps1 -PkiRoot ./pki -Force
   ```
   Files created in `pki/own/`.

2. **Seed a rejected certificate** — simulate an untrusted server cert:
   ```powershell
   Copy-Item pki/own/client.current.cer pki/rejected/test-server.cer
   ```

3. **Start the app and navigate to the Certificates tab**:
   ```powershell
   cd src/PlcLab.Web
   dotnet run
   ```
   - **Rejected** pane shows `test-server.cer`.
   - Click **Trust** → cert moves to **Trusted** pane.
   - Click **Reject** to move it back; click **Delete** to remove it.

> The PKI Root badge at the top of the panel should now show the local `pki/` path instead of `/app/pki`.

## Docker OPC UA Reference Server trust flow

1. Start services: `docker compose up -d`.
2. Connect PlcLab to the OPC UA endpoint.
3. Inspect client rejected certificates in the Certificates tab.
4. Validate certificate identity (thumbprint/subject) using a trusted out-of-band source.
5. Promote certificate to trusted.
6. Reconnect and verify secure session establishment.

For persistent client trust state, mount a host volume to the app PKI path.

## Revoke or quarantine certificates

- Move trusted certificate back to rejected using the UI (Reject).
- Delete from rejected to fully remove local trust artifacts.
- Re-issue/rotate client certificate if compromise is suspected.

## Renewal recommendation

- Rotate client certificate at least every 12 months.
- Rotate immediately after key exposure, team member offboarding, or incident response.
- Keep archived certificates for audit and rollback analysis.

## How it works

### `useSecurity: false` (default)

The **Use TLS/Certificate security** toggle in the UI is off by default. In this mode `OpcSessionAdapter` calls `GetEndpoints()` on the server and selects an endpoint where `SecurityMode == None`:

- No certificate exchange happens
- The `pki/trusted` and `pki/rejected` folders are not consulted
- If the server has no unsecured endpoint the connection throws `BadSecurityPolicyRejected`

### `useSecurity: true` (toggle on)

When you enable the toggle the adapter uses `CoreClientUtils.SelectEndpointAsync` to pick the server's highest-security endpoint:

1. The client presents its own certificate from `pki/own` to the server
2. The server presents its certificate to the client
3. Because `AutoAcceptUntrustedCertificates = false`, an unknown server cert is **rejected** and written to `pki/rejected`
4. The connection fails until you promote that cert to `pki/trusted` using the **Certificates** tab
5. On the next reconnect the trusted cert is found and a secure session is established

### What the Certificates UI manages

The **Certificates** tab exists to handle step 3–4 above. It reads directly from the `pki/trusted` and `pki/rejected` directories configured by `OpcUa:PkiRootPath`. Actions:

| Action | Effect |
|--------|--------|
| **Trust** | Moves a cert from `rejected/` → `trusted/` so the next secure connect succeeds |
| **Reject** | Moves a cert from `trusted/` → `rejected/` to revoke local trust |
| **Delete** | Permanently removes a cert from `rejected/` |