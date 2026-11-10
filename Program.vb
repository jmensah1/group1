Imports System.IO

Module Program
    <STAThread>
    Public Sub Main(args As String())
        If args.Length > 0 AndAlso args(0) = "--self-test" Then
            SelfTests.Run(args(1))
            Return
        End If
        If args.Length > 0 AndAlso args(0) = "--race-worker" Then
            SelfTests.RaceWorker(args(1))
            Return
        End If
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2)
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        If args.Length = 2 AndAlso args(0) = "--layout-test" Then
            LayoutChecks.Run(args(1))
            Return
        End If
        If args.Length >= 3 AndAlso args(0) = "--ui-test" Then
            ' Visual QA runs with its own explicitly supplied data folder.
            DataStore.Root = Path.Combine(args(2), "Data")
            DataStore.Initialize()
            Dim workspace As New WorkspaceForm(DataStore.Login(args(1), "Demo@123"))
            If args.Length = 4 AndAlso args(3) = "small" Then workspace.Size = workspace.MinimumSize
            Application.Run(workspace)
            Return
        End If
        AddHandler Application.ThreadException, Sub(s, e) MessageBox.Show(e.Exception.Message, "Action could not complete", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        Try
            DataStore.Initialize()
            Application.Run(New Form1())
        Catch ex As Exception
            MessageBox.Show("The demo could not start. " & ex.Message & Environment.NewLine & "Your data has been preserved.", "Group1 Transport", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
End Module
