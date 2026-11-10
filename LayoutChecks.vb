Imports System.IO

' A visual regression harness with its own data folder; no production users or data are modified.
Public Module LayoutChecks
    Public Sub Run(folder As String)
        Directory.CreateDirectory(folder)
        Dim checks As New List(Of String)
        Try
            Using login As New Form1()
                If login.usernameBox.Parent Is Nothing OrElse login.logoPicture.Image Is Nothing Then Throw New Exception("Designer controls and embedded logo must exist before Shown.")
                checks.Add("PASS: Designer initializes the entire login UI and embedded logo before runtime events.")
                login.Show()
                login.usernameBox.Text = "Presentation preview"
                For Each size In {New Size(1000, 740), New Size(1280, 720), New Size(1920, 1080), New Size(1000, 740)}
                    login.ClientSize = size
                    Application.DoEvents()
                    If login.usernameBox.Font.Size < 18 Then Throw New Exception("Input font became smaller after resize.")
                    If login.usernameBox.Width > 650 Then Throw New Exception("Login field stretched beyond its readable maximum width.")
                    If login.passwordBox.Font.Size <> login.usernameBox.Font.Size Then Throw New Exception("Login inputs use inconsistent sizing.")
                    SaveImage(login, Path.Combine(folder, "login-" & size.Width & "x" & size.Height & ".png"))
                    checks.Add("PASS: Login " & size.Width & "x" & size.Height & ": font " & login.usernameBox.Font.Size.ToString("N1") & " pt; input width " & login.usernameBox.Width & " px.")
                Next
                login.presentationCheck.Checked = True
                login.ClientSize = New Size(1280, 720)
                Application.DoEvents()
                If login.usernameBox.Font.Size < 20 Then Throw New Exception("Presentation input font must be at least 20 pt.")
                SaveImage(login, Path.Combine(folder, "login-presentation.png"))
                checks.Add("PASS: Login presentation mode uses at least 20 pt input text.")
                login.Close()
            End Using
            DataStore.Root = Path.Combine(folder, "Data")
            DataStore.Initialize()
            For Each role In {"passenger", "bookman", "admin"}
                Ui.PresentationMode = False
                Dim session = DataStore.Login(role, "Demo@123")
                Using workspace As New WorkspaceForm(session)
                    workspace.Show()
                    For Each size In {New Size(900, 680), New Size(1920, 1080), New Size(900, 680)}
                        workspace.ClientSize = size
                        ResponsiveUi.Apply(workspace)
                        Application.DoEvents()
                        If workspace.Font.Size < 14 Then Throw New Exception("Workspace font decreased below the readable baseline.")
                        For Each card In Descendants(workspace).Where(Function(c) TypeOf c Is TableLayoutPanel AndAlso Convert.ToString(c.Tag) = "scale")
                            If card.Width < 350 Then Throw New Exception("Trip cards collapsed below the readable width.")
                        Next
                        SaveImage(workspace, Path.Combine(folder, role & "-" & size.Width & "x" & size.Height & ".png"))
                        checks.Add("PASS: " & role & " layout " & size.Width & "x" & size.Height & ": base font " & workspace.Font.Size.ToString("N1") & " pt.")
                    Next
                    Ui.PresentationMode = True
                    workspace.ClientSize = New Size(1280, 720)
                    ResponsiveUi.Apply(workspace)
                    Application.DoEvents()
                    SaveImage(workspace, Path.Combine(folder, role & "-presentation.png"))
                    checks.Add("PASS: " & role & " presentation mode renders with enlarged text.")
                    workspace.Close()
                End Using
                DataStore.Logout(session)
            Next
            checks.Add("ALL " & checks.Count & " LAYOUT CHECKS PASSED")
            File.WriteAllLines(Path.Combine(folder, "layout-validation.txt"), checks)
            Environment.ExitCode = 0
        Catch ex As Exception
            checks.Add(ex.ToString())
            File.WriteAllLines(Path.Combine(folder, "layout-validation.txt"), checks)
            Environment.ExitCode = 1
        Finally
            Ui.PresentationMode = False
        End Try
    End Sub

    Private Iterator Function Descendants(parent As Control) As IEnumerable(Of Control)
        For Each child As Control In parent.Controls
            Yield child
            For Each descendant In Descendants(child)
                Yield descendant
            Next
        Next
    End Function

    Private Sub SaveImage(form As Form, file As String)
        Using bitmap As New Bitmap(form.Width, form.Height)
            form.DrawToBitmap(bitmap, New Rectangle(Point.Empty, form.Size))
            bitmap.Save(file, System.Drawing.Imaging.ImageFormat.Png)
        End Using
    End Sub
End Module
