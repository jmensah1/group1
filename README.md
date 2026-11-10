# Group1 Transport — classroom demo

VB.NET Windows Forms · .NET 10 · JSON storage · QRCoder 1.8.1

## Open and run

Open `Group1.slnx` in Visual Studio 2026 and press **F5** (or Ctrl+F5). The .NET desktop development workload and .NET 10 SDK are required; both were present on the development PC. NuGet restores QRCoder on the first build. The ready-to-run framework-dependent build is also supplied in the delivery's `ReadyToRun` folder; keep all its files together and run `Group1.exe`. It needs the .NET 10 Windows Desktop runtime.

| Username | Password | Workspace |
|---|---|---|
| `passenger` | `Demo@123` | Search trips, book, view QR tickets, cancel |
| `bookman` | `Demo@123` | Accra counter, live seats, boarding, trip status |
| `admin` | `Demo@123` | Dashboard, operations, buses, routes, trips, users, audit |

These are public classroom demo credentials. Register creates Passenger accounts only. Admins create staff accounts, change roles, set passwords and disable accounts. Leave an existing user's password blank when editing to keep it.

## Five-minute presentation

1. Start two copies **of the same executable in the same folder**. Sign in as passenger in one and bookman in the other. Data is shared by executable folder, and refreshes every four seconds. Separate copied folders use separate data.
2. Passenger: choose Accra → Kumasi, Book ticket, enter a valid Ghana phone, choose MoMo. A simulated approval screen waits 2.6 seconds. A mock SMS popup appears and is recorded in `smslog.json`. Show the QR ticket and Print preview. No real payment or SMS is sent.
3. Use **Copy QR text** on the ticket. Bookman: sell a Cash ticket, then open Validate boarding, paste the passenger's signed QR text, and Validate. Paste it again to demonstrate **Already used**.
4. Fill the five-seat Accra → Kumasi bus. Full appears and booking/selling is disabled in both copies within four seconds. A stale button is also safe: the data service rechecks capacity when committing the sale.
5. Cancel an unused ticket from My tickets to free a seat. A Boarded ticket cannot be cancelled. Pay at station reserves a seat immediately; the bookman must tick cash collected before boarding that ticket.
6. Admin: show revenue, today's tickets, occupancy and loading metrics; the tickets-per-route bar chart; Live operations; sortable Routes headers and destination filter; and Audit log. Resize and maximize the windows.

The first bus deliberately has **5 demo seats** so Full can be demonstrated quickly. The other buses have 45, 45 and 38 seats. Admin can edit capacity, but never below seats already sold.

## Operational rules

- Booked, Paid and Boarded tickets consume capacity. Cancelled and Expired tickets do not.
- Every sale uses one service method protected by an OS file lock across processes. It reloads committed data, authorizes the actor, checks capacity and status, then commits.
- Full is derived automatically at capacity. You cannot manually declare a partly sold bus Full. Cancelling an unused ticket reopens the preceding Scheduled or Loading status.
- Staff advance Scheduled → Loading → Departed; a Full trip can also Depart. Departed trips reject sales and boarding. Unpaid Booked tickets expire on departure. Paid no-shows remain Paid in the historical ledger and cannot board a departed bus.
- Green seat count means below 90%; amber means at least 90%; red means full. Loading has a blue status indicator; Departed is dark slate.
- Bookmen serve their assigned origin station. Admins see all stations. A trip needs an active bookman from its origin station.
- Editing a route's fare affects new sales only. Historical ticket amounts stay unchanged.
- Buses cannot be double-scheduled within three hours. This is a classroom scheduling guard, not a real travel-time optimizer.
- Trips with tickets retain their route, bus and departure time. Linked records cannot be deleted; accounts are disabled to preserve history.
- Cash is marked collected at counter sale. Pay at station stays pending until a bookman confirms cash before boarding. Cancellations mark paid transactions Refunded (mock); there is no real refund operation.
- Revenue sums successful payments, excluding mock refunds. Tickets sold today excludes cancellations. Average occupancy is the mean occupancy percentage of non-departed trips. The bar chart counts tickets excluding Cancelled and Expired.

## JSON, crash recovery and reset

On first run, `Data` is created next to the executable with `users.json`, `stations.json`, `routes.json`, `buses.json`, `trips.json`, `tickets.json`, `payments.json`, `smslog.json` and `auditlog.json`. These are human-readable projections of the shared state.

`Data/state.json` is the authoritative transaction snapshot. Each commit flushes a temporary file to disk, atomically replaces the snapshot, then replaces the individual JSON files. This provides a single commit point across the nine related files. On startup, projections are repaired from the snapshot if a previous process stopped during projection writes. Do not manually edit the projection files: the next commit rewrites them. Corrupt or missing authoritative state is reported; existing data is never silently reseeded.

`Data/.transaction.lock` is a cross-process file lock. Do not remove it while apps are running. The OS releases its handle automatically on process exit or crash. Pending operations wait up to 15 seconds before reporting that data is busy.

`Data_Seed` holds the original seed snapshot and its nine JSON projections. **Admin → Overview → Reset demo data** first preserves the current snapshot in `Backups`, then regenerates the same clean sample records with tomorrow's departures, fresh password salts and a fresh signing key. All current sessions, including sessions in other app copies, must sign in again. The reset is logged. This avoids stale dates when presenting on another day.

To back up manually, close all copies and copy the complete `Data` folder. To restore a backup snapshot, close all copies, preserve the existing folder, and restore the snapshot as `Data/state.json`; the next launch recreates its projections. Treat Data and Backups as sensitive local files. They include passenger details and the QR signing key.

## Security and limits

Passwords use `Rfc2898DeriveBytes.Pbkdf2` (SHA-256, 150,000 iterations, individual 128-bit salts); hashes are compared in constant time. Five incorrect passwords lock the account for one minute, stored in JSON so another app copy cannot bypass the lock. Usernames are unique without regard to case.

Every mutation checks the current persisted account and role in the service, including active-account and station checks. Passenger snapshots omit other passengers' tickets, password hashes and signing secrets. Ticket numbers contain 12 cryptographically random hexadecimal characters after `GH-`. QR text includes the ticket number, trip ID and HMAC-SHA256 signature; modified signatures fail. A typed ticket number is checked against the stored ticket and can board once. A real deployment would also need a trusted scanning endpoint and tighter access controls.

This local JSON demo assumes the PC user and its local files are trusted. It is not a security boundary against someone who can edit program/data files. There are no MoMo or SMS API keys and no external services. A real version would put secrets in a protected secret store, use a transactional server database, and perform authorization and payment confirmation on a server.

Print preview uses `PrintDocument` and `PrintPreviewDialog`; Windows needs an installed print driver (for example Microsoft Print to PDF). Physical printing is optional for the demo.

Future work: real MoMo/SMS, GPS, seat numbers, multiple companies, web/mobile apps, PostgreSQL, payment reconciliation, production security and monitoring.

## Source map / group split

| Member | Main files | Responsibility |
|---|---|---|
| 1 | `Models.vb`, `DataStore.vb`, `Program.vb`, `Form1.vb` | Models, persistence, authentication, registration, auditing |
| 2 | `WorkspaceForm.vb`, `TicketForm.vb` | Passenger search, booking, MoMo, SMS and QR tickets |
| 3 | `WorkspaceForm.vb`, `TicketForm.vb` | Station cards, cash sales, validation, status and printing |
| 4 | `AdminEditors.vb`, `WorkspaceForm.vb`, `Ui.vb` | Management, grids, dashboard, chart and responsive controls |

The login screen is fully defined in `Form1.Designer.vb`, including its embedded logo, text boxes and buttons, so it is visible and editable in the Visual Studio designer. Runtime login behavior and bounded responsive sizing live in `Form1.vb`. Other screens use reusable Dock, TableLayoutPanel and FlowLayoutPanel controls. `ResponsiveUi.vb` scales readable fonts and their containers together.

## Validation

`dotnet build Group1.vbproj` must report zero errors and zero warnings. Automated functional/security/concurrency checks are in `SelfTests.vb`. To rerun, execute `Group1.exe --self-test "C:\some\new\isolated-test-folder"`; use a fresh folder each time. This mode writes `validation.txt` and exits with code 0 for success. It does not use the application's normal Data folder. The test launches three hidden seller processes that attempt 60 sales against five seats.

For visual QA only: `Group1.exe --ui-test passenger "C:\some\isolated-qa-folder"` opens a demo workspace with data in that explicitly supplied folder; `bookman` and `admin` work likewise. This uses the public seeded credentials and does not grant access to a normal signed-in user's session. Normal launches always show the login screen.

QRCoder package and license: https://www.nuget.org/packages/QRCoder/1.8.1 (MIT). There are no image assets or web services required to run the application.

## Presentation and visual refresh

Login inputs start at 18 pt and grow with larger windows while their width stays bounded. The login checkbox **Presentation mode** and the workspace gold **Presentation: On/Off** button enlarge text for a projector. Workspaces start with a 14 pt base font, increase toward 20 pt at full HD, and can be enlarged further with presentation mode. Smaller windows keep readable text and use wrapping or scrolling instead of shrinking it. Repeated maximize/restore does not compound font scaling.

The generated bus-and-road logo is embedded in Form1.resx and included in Assets/group1-logo.png; keep the Assets folder with the source. No separate image files are needed beside the compiled app. The palette uses navy, teal, blue and gold with status colours on trip cards.

Run Group1.exe --layout-test with a new isolated output folder as the second argument to render and check the login and all role workspaces at small, projector and full-HD sizes. The visual report is LAYOUT-VALIDATION.txt in this delivery.

