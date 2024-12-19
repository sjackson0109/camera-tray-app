Imports System.IO
Imports System.Diagnostics
Imports System.Drawing
Imports System.Environment
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports System.Text.Json
Imports System.Windows.Forms 
 
Public Module Program
    Public config As configData = LoadConfig()

    Sub Main(args As String())
        Application.EnableVisualStyles()
        LogUtility.EnsureDirectoryExists(LogUtility.LogDirectory)
        LogUtility.WriteLog("info", "Application started successfully.")
        If args.Length > 0 Then ' Process arguments
            Dim CameraTrayApp = new CameraTrayApp()
            LogUtility.WriteLog("trace", "CLI argument `{args(0)}` identified")
            Select Case args(0).ToLower().Replace("-","")
                Case "about"
                    CameraTrayApp.btnAbout()
                Case "show"
                    CameraTrayApp.btnShow()
                Case "config"
                    CameraTrayApp.btnConfig()
                Case Else
                    MessageBox.Show("Unknown argument. Valid options: 'show', 'about', 'config'.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Select
        Else
            Application.Run(New CameraTrayApp())
        End If
    End Sub
End Module

Public Module LogUtility
    Public ReadOnly LogDirectory As String = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "\", _
        Assembly.GetExecutingAssembly().GetCustomAttribute(Of AssemblyCompanyAttribute)?.Company.ToString(), "\", _
        Assembly.GetExecutingAssembly().GetCustomAttribute(Of AssemblyProductAttribute)?.Product.ToString(), "\logs")
    Public ReadOnly LogFilePath As String = System.IO.Path.Combine(LogDirectory, "application.log")    
    Private ReadOnly LogLevels As New Dictionary(Of String, Integer) From { {"trace", 1}, {"info", 2}, {"warning", 3}, {"error", 4} }

    ' Existing method to ensure directory exists recursively
    Public Sub EnsureDirectoryRecursive(dir As String)
        If String.IsNullOrEmpty(dir) OrElse System.IO.Directory.Exists(dir) Then
            Return ' Base case: Stop if the directory exists or is invalid
        End If

        ' Recurse to ensure the parent directory exists
        Dim parentDirectory As String = System.IO.Path.GetDirectoryName(dir)
        EnsureDirectoryRecursive(parentDirectory)

        ' Create the current directory after parent is ensured
        System.IO.Directory.CreateDirectory(dir)
    End Sub
    ' Recursive helper function to ensure all levels of the directory exist
    Public Sub EnsureDirectoryExists(filePath As String)
        Try
            ' Extract the target directory
            Dim directory As String = System.IO.Path.GetDirectoryName(filePath)

            ' Ensure the target directory recursively
            EnsureDirectoryRecursive(directory)
        Catch ex As Exception
            ' Log or handle errors as needed
            Throw
        End Try
    End Sub

    Public Sub WriteLog(level As String, message As String)
        Try
            ' Skip writing logs if the level is lower than the configured level
            If Not LogLevels.ContainsKey(level.ToLower()) OrElse
                LogLevels(level.ToLower()) < LogLevels(Program.config.debugLevel) Then
                Return
            End If

            ' Construct log file path
            Dim logFilePath As String = System.IO.Path.Combine( GetFolderPath(SpecialFolder.ApplicationData),Assembly.GetExecutingAssembly().GetCustomAttribute(Of AssemblyCompanyAttribute)?.Company.ToString(),"CameraTrayApp","application.log" )
            Dim logMessage As String = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level.ToLower()}] {message}"

            ' Ensure the logs directory exists
            Dim logDirectory As String = System.IO.Path.GetDirectoryName(logFilePath)
            If Not IO.Directory.Exists(logDirectory) Then
                IO.Directory.CreateDirectory(logDirectory)
            End If

            ' Write to log file
            IO.File.AppendAllText(logFilePath, logMessage & Environment.NewLine)

            ' Optionally write to the console for debugging
            Console.WriteLine(logMessage)
        Catch ex As Exception
            ' Fallback if logging fails
            Console.WriteLine($"Failed to write log: {ex.Message}")
        End Try
    End Sub

End Module

Public Class TrayIconUtility
    Private trayIcon As NotifyIcon
    Private cameraApp As CameraTrayApp
    Public Sub New()
        Try
            WriteLog("info", "Initializing TrayIconUtility...")
            cameraApp = New CameraTrayApp()
            trayIcon = New NotifyIcon() With { .Icon = New Icon("CameraTrayApp.ico"), .Visible = True, .Text = Assembly.GetExecutingAssembly().GetCustomAttribute(Of AssemblyTitleAttribute)()?.Title.ToString() }
            WriteLog("info", "TrayIconUtility initialized successfully.")
        Catch ex As Exception
            WriteLog("error", $"Error initializing TrayIconUtility: {ex.Message}")
            Throw
        End Try
    End Sub
End Class


Public Class frmAbout
    Inherits Form
    Private memoField As TextBox
    Private authorLabel As Label
    Private versionLabel As Label
    Private sourceLabel As Label
    Private hyperlink As LinkLabel

    Public Sub New()
        Try
            WriteLog("info", "Initializing About form...")

            ' Set form properties
            Me.Text = "About"
            Me.Size = New Size(370, 170)
            Me.StartPosition = FormStartPosition.CenterScreen

            memoField = New TextBox() With { .Location = New Point(20, 20), .Size = New Size(315, 40), .ReadOnly = True, .Multiline = True, .ScrollBars = ScrollBars.None, .Text = "A lightweight system tray application for managing and viewing RTSP camera feeds." }
            authorLabel = New Label() With { .Location = New Point(20, 65), .AutoSize = True, .Text = "Author: " & Assembly.GetExecutingAssembly().GetCustomAttribute(Of AssemblyCompanyAttribute)()?.Company.ToString() }
            versionLabel = New Label() With { .Location = New Point(20, 85), .AutoSize = True, .Text = "Version: " & Assembly.GetExecutingAssembly().GetName().Version.ToString() }
            sourceLabel = New Label() With { .Location = New Point(20, 105), .AutoSize = True, .Text = "Source:" }
            hyperlink = New LinkLabel() With { .Location = New Point(63, 105), .AutoSize = True, .Text = "github.com/sjackson0109/camera-tray-app" }

            Me.Controls.Add(memoField)
            Me.Controls.Add(authorLabel)
            Me.Controls.Add(versionLabel)
            Me.Controls.Add(sourceLabel)
            Me.Controls.Add(hyperlink)

            AddHandler hyperlink.LinkClicked, AddressOf HyperlinkClicked
            AddHandler Me.Load, AddressOf frmAbout_Load

            WriteLog("info", "About form initialized successfully.")
        Catch ex As Exception
            WriteLog("error", $"Error initializing About form: {ex.Message}")
            Throw
        End Try
    End Sub

    Private Sub frmAbout_Load(sender As Object, e As EventArgs)
        Try
            WriteLog("info", "About form loaded.")
            ' Clear selection from memoField - no more blue highlighted text
            memoField.Select(0, 0)
            Me.ActiveControl = Nothing
        Catch ex As Exception
            WriteLog("error", $"Error during About form load: {ex.Message}")
        End Try
    End Sub

    Private Sub HyperlinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs)
        Try
            WriteLog("info", "Hyperlink clicked. Opening GitHub page.")
            Process.Start(New ProcessStartInfo("https://github.com/sjackson0109/camera-tray-app") With { .UseShellExecute = True })
        Catch ex As Exception
            WriteLog("error", $"Error opening hyperlink: {ex.Message}")
        End Try
    End Sub
End Class


Public Class frmConfig
    Inherits Form

    Public Property UpdatedCameras As IEnumerable(Of Object)

    Private Shadows saveButton As Button
    Private Shadows cancelButton As Button
    Private camerasTable As DataGridView
    Private memo As Label

    Public Sub New()
        Try
            LogUtility.WriteLog("trace", "Initializing Configure Cameras form...")

            ' Initialize form
            Dim config = ConfigUtility.LoadConfig()
            Console.WriteLine($"Loaded config with {config.Cameras?.Count} cameras.")

            Dim headerLabel As New Label() With {.Text = "Camera Configuration"}
            Me.Text = "Configure Cameras"
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.MinimumSize = New Size(845, 250)

            ' Create DataGridView
            camerasTable = New DataGridView() With {
                .Dock = DockStyle.Top, 
                .Size = New Size(845, 120), 
                .MaximumSize = New Size(845, 120), 
                .AllowUserToAddRows = True, 
                .AllowUserToDeleteRows = True, 
                .AutoGenerateColumns = False,
                .ScrollBars = ScrollBars.Vertical
            }
            LogUtility.WriteLog("trace", "DataGridView initialized.")

            ' Add columns dynamically based on configuration properties (width=785, plus left navigation columns)
            camerasTable.Columns.Add(New DataGridViewTextBoxColumn() With {.Width = 120, .HeaderText = "Name", .DataPropertyName = "Name"})
            camerasTable.Columns.Add(New DataGridViewTextBoxColumn() With {.Width = 350, .HeaderText = "URL", .DataPropertyName = "Url"})
            camerasTable.Columns.Add(New DataGridViewTextBoxColumn() With {.Width = 80, .HeaderText = "Username", .DataPropertyName = "Username"})
            camerasTable.Columns.Add(New DataGridViewTextBoxColumn() With {.Width = 80, .HeaderText = "Password", .DataPropertyName = "Password"})
            camerasTable.Columns.Add(New DataGridViewTextBoxColumn() With {.Width = 30, .HeaderText = "X", .DataPropertyName = "X"})
            camerasTable.Columns.Add(New DataGridViewTextBoxColumn() With {.Width = 30, .HeaderText = "Y", .DataPropertyName = "Y"})
            camerasTable.Columns.Add(New DataGridViewTextBoxColumn() With {.Width = 45, .HeaderText = "Width", .DataPropertyName = "Width"})
            camerasTable.Columns.Add(New DataGridViewTextBoxColumn() With {.Width = 50, .HeaderText = "Height", .DataPropertyName = "Height"})

            ' Bind cameras to the DataGridView
            Dim bindingSource As New BindingSource()
            bindingSource.DataSource = config.Cameras
            camerasTable.DataSource = bindingSource
            Me.Controls.Add(camerasTable)
            LogUtility.WriteLog("trace", "DataGridView columns and binding completed.")

            ' Add additional information label
            Dim infoLabel As New Label() With {
                .Dock = DockStyle.None,
                .Location = New Point(10, 120), .AutoSize = True, 
                .Text = "Please input the following information for each camera:" & vbCrLf &
                        "- Name: A descriptive name for the camera." & vbCrLf &
                        "- URL: The RTSP URL for the camera feed." & vbCrLf &
                        "- Protocol: TCP or UDP (default)." & vbCrLf &
                        "- Username: (Optional) RTSP username for the camera feed." & Environment.NewLine &
                        "- Password: (Optional) RTSP password for the camera feed." & Environment.NewLine &
                        "- X, Y: Coordinates for the camera feed placement." & Environment.NewLine &
                        "- Width, Height: Dimensions of the camera feed window."
            }
            Me.Controls.Add(infoLabel)
            LogUtility.WriteLog("trace", "Information label added.")

            ' Add Save and Cancel buttons
            Dim buttonPanel As New FlowLayoutPanel() With {
                .Dock = DockStyle.Bottom,
                .FlowDirection = FlowDirection.RightToLeft,
                .Height = 30
            }
            saveButton = New Button() With {.Text = "Save"}
            cancelButton = New Button() With {.Text = "Cancel"}
            
            AddHandler saveButton.Click, AddressOf OnSave
            AddHandler cancelButton.Click, AddressOf OnCancel

            buttonPanel.Controls.AddRange(New Control() {saveButton, cancelButton})
            Me.Controls.Add(buttonPanel)
            LogUtility.WriteLog("trace", "Save and Cancel buttons added.")

            ' Set form dimensions
            AdjustFormSize(camerasTable, headerLabel)
            LogUtility.WriteLog("trace", "Form size adjusted.")

            ' Clone the cameras for editing
            Me.UpdatedCameras = If(config?.Cameras IsNot Nothing,
                config.Cameras.Select(Function(c)
                    Return New Dictionary(Of String, Object) From { {"Name", c.Name}, {"Url", c.Url},{"Protocol", c.Protocol}, {"Username", c.Username}, {"Password", c.Password}, {"X", c.X}, {"Y", c.Y}, {"Width", c.Width}, {"Height", c.Height} }
                End Function).Cast(Of Object).ToList(),
                New List(Of Object)())
            LogUtility.WriteLog("trace", "Cameras cloned for editing.")

        Catch ex As Exception
            msgbox("Error initializing Configure Cameras form: {ex.Message}", vbOKOnly, "error")
            LogUtility.WriteLog("error", $"Error initializing Configure Cameras form: {ex.Message}")
            Throw
        End Try
    End Sub

    Private Sub AdjustFormSize(dataGrid As DataGridView, headerLabel As Label)
        Try
            If dataGrid Is Nothing Then Throw New ArgumentNullException(NameOf(dataGrid), "DataGridView cannot be null.")
            If headerLabel Is Nothing Then Throw New ArgumentNullException(NameOf(headerLabel), "HeaderLabel cannot be null.")
            ' Adjust form size logic here
            headerLabel.Width = Me.ClientSize.Width
            dataGrid.Width = Me.ClientSize.Width
            dataGrid.Height = Me.ClientSize.Height - headerLabel.Height
            LogUtility.WriteLog("trace", "Form size adjusted in AdjustFormSize method.")
        Catch ex As Exception
            LogUtility.WriteLog("error", $"Error in AdjustFormSize: {ex.Message}")
            Throw
        End Try
    End Sub

    Private Sub OnSave(sender As Object, e As EventArgs)
        Try
            ' Save updates to cameras
            Me.UpdatedCameras = CType(camerasTable.DataSource, BindingSource).List.Cast(Of Object).ToList()
            Me.DialogResult = DialogResult.OK
            LogUtility.WriteLog("info", "Camera Configuration SAVED")
            Me.Close()
        Catch ex As Exception
            LogUtility.WriteLog("error", $"Error saving camera configuration: {ex.Message}")
            Throw
        End Try
    End Sub

    Private Sub OnCancel(sender As Object, e As EventArgs)
        Try
            ' Discard changes and close the form
            Me.DialogResult = DialogResult.Cancel
            LogUtility.WriteLog("info", "Camera Configuration CANCELLED")
            Me.Close()
        Catch ex As Exception
            LogUtility.WriteLog("error", $"Error cancelling camera configuration: {ex.Message}")
            Throw
        End Try
    End Sub

End Class


Public Class frmShow
    Inherits Form

    ' Declare necessary external functions
    <DllImport("user32.dll", SetLastError:=True, CharSet:=CharSet.Auto)>
    Private Shared Function EnumWindows(lpEnumFunc As EnumWindowsProc, lParam As IntPtr) As Boolean
    End Function

    <DllImport("user32.dll", CharSet:=CharSet.Auto)>
    Private Shared Function GetWindowText(hWnd As IntPtr, lpString As System.Text.StringBuilder, nMaxCount As Integer) As Integer
    End Function

    <DllImport("user32.dll")>
    Private Shared Function SetParent(hWndChild As IntPtr, hWndNewParent As IntPtr) As IntPtr
    End Function

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function MoveWindow(hWnd As IntPtr, X As Integer, Y As Integer, nWidth As Integer, nHeight As Integer, bRepaint As Boolean) As Boolean
    End Function

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function SetWindowLong(hWnd As IntPtr, nIndex As Integer, dwNewLong As Integer) As Integer
    End Function

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function GetWindowLong(hWnd As IntPtr, nIndex As Integer) As Integer
    End Function

    Private Const GWL_STYLE As Integer = -16
    Private Const WS_VISIBLE As Integer = &H10000000
    Private Delegate Function EnumWindowsProc(hWnd As IntPtr, lParam As IntPtr) As Boolean
    Private ReadOnly Cameras As List(Of Object)
    Private ffplayProcesses As New List(Of Process)()

    Public Sub New()
        Try
            Dim config = ConfigUtility.LoadConfig()
            If config Is Nothing Then
                Throw New Exception("Configuration file is invalid or missing camera data.")
            End If
            Me.Cameras = config.Cameras.Cast(Of Object).ToList()
            WriteLog("info", "Initializing frmShow.")
            Me.Text = "Camera Feeds"
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.WindowState = FormWindowState.Normal ' Starting state
            Me.FormBorderStyle = FormBorderStyle.SizableToolWindow ' Remove border and buttons
            If Cameras.Count <= 1 Then
                Me.Size = New Size(800, 580)
            Else
                Me.Size = CalculateGridSize()
            End If
            AddHandler Me.Resize, AddressOf OnFormResize
            SetupGrid()
            WriteLog("info", "frmShow initialized successfully.")
        Catch ex As Exception
            WriteLog("error", $"Error initializing frmShow: {ex.Message}")
            Throw
        End Try
    End Sub

    Private Sub SetupGrid()
        Try
            WriteLog("info", "Setting up grid.")
            If Cameras Is Nothing OrElse Cameras.Count = 0 Then ' Check if Cameras has records
                Throw New InvalidOperationException("No cameras configured. Please add camera records in the configuration.")
            End If
            Me.Controls.Clear() ' Clear previous controls
            Dim columns As Integer = If(Cameras.Count = 1, 1, Math.Max(1, CInt(Math.Ceiling(Math.Sqrt(Cameras.Count)))))
            Dim rows As Integer = If(Cameras.Count = 1, 1, CInt(Math.Ceiling(Cameras.Count / columns)))
            Dim panelWidth As Integer = Me.ClientSize.Width \ columns
            Dim panelHeight As Integer = Me.ClientSize.Height \ rows
            For i As Integer = 0 To Cameras.Count - 1 ' looping through configured camera configs
                Dim camera As CameraConfig = DirectCast(Cameras(i), CameraConfig)
                Dim panel As New Panel With { .Width = panelWidth, .Height = panelHeight, .Left = (i Mod columns) * panelWidth, .Top = (i \ columns) * panelHeight, .BackColor = Color.Black }
                Me.Controls.Add(panel)
                StartFFplay(camera, panel,"") '3rd parameter includes optional parameters for ffplay.exe
                'StartFFplay(rtspUrl, panel, Cameras(i).Name)
            Next

            WriteLog("info", "SetupGrid completed successfully.")
        Catch ex As InvalidOperationException
            WriteLog("error", ex.Message)
            MessageBox.Show(ex.Message, "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        Catch ex As Exception
            WriteLog("error", $"Error setting up grid: {ex.Message}")
            Throw
        End Try
    End Sub
    Private Sub OnFormResize(sender As Object, e As EventArgs)
        Try
            Dim columns As Integer = If(Cameras.Count = 1, 1, Math.Max(1, CInt(Math.Ceiling(Math.Sqrt(Cameras.Count)))))
            Dim rows As Integer = If(Cameras.Count = 1, 1, CInt(Math.Ceiling(Cameras.Count / columns)))

            Dim panelWidth As Integer = Me.ClientSize.Width \ columns
            Dim panelHeight As Integer = Me.ClientSize.Height \ rows

            For i As Integer = 0 To Cameras.Count - 1
                Dim panel = DirectCast(Me.Controls(i), Panel)
                If panel IsNot Nothing Then
                    panel.Width = panelWidth
                    panel.Height = panelHeight
                    panel.Left = (i Mod columns) * panelWidth
                    panel.Top = (i \ columns) * panelHeight

                    Dim camera As CameraConfig = DirectCast(Cameras(i), CameraConfig)
                    Dim handle = FindWindowByPartialTitle(camera.Name)
                    If handle <> IntPtr.Zero Then
                        MoveWindow(handle, 0, 0, panel.Width, panel.Height, True)
                    End If
                End If
            Next
        Catch ex As Exception
            WriteLog("error", $"Error resizing grid: {ex.Message}")
        End Try
    End Sub
    Private Function CalculateGridSize() As Size
        ' Default to 800x600 for single feed or calculate based on grid
        If Cameras.Count = 1 Then
            Return New Size(800, 600) ' Adjust to fit single video resolution
        End If

        Dim feedWidth As Integer = 400
        Dim feedHeight As Integer = 300
        Dim gridColumns As Integer = Math.Max(1, CInt(Math.Ceiling(Math.Sqrt(Cameras.Count))))
        Dim gridRows As Integer = CInt(Math.Ceiling(Cameras.Count / CDbl(gridColumns)))

        Dim totalWidth As Integer = gridColumns * feedWidth
        Dim totalHeight As Integer = gridRows * feedHeight

        Return New Size(totalWidth, totalHeight)
    End Function
    Public Sub GenerateRtspUrls(cameras As List(Of Object))
        For Each cameraObj In cameras
            Dim camera As CameraConfig = DirectCast(cameraObj, CameraConfig)
            Dim rtspUrl As String = ConstructRtspUrl(camera)
            Console.WriteLine($"RTSP URL for {camera.Name}: {rtspUrl}")
        Next
    End Sub
    Private Function ConstructRtspUrl(camera As CameraConfig) As String
        Dim uri As New Uri(camera.Url)
        Dim port As Integer = If(uri.IsDefaultPort, 554, uri.Port)
        Return $"{uri.Scheme}://{camera.Username}:{camera.Password}@{uri.Host}:{port}{uri.PathAndQuery}"
    End Function
    'Private Sub StartFFplay(rtspUrl As String, targetPanel As Panel, cameraName As String, Optional additionalArgs As String = "")
    Private Sub StartFFplay(camera As CameraConfig, targetPanel As Panel, Optional additionalArgs As String = "")
        Try
            Dim ffplayPath As String = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lib", "ffplay.exe")
            ' Verify if ffplay.exe exists
            If Not File.Exists(ffplayPath) Then
                WriteLog("error", $"ffplay executable not found at path '{ffplayPath}'")
                Throw New FileNotFoundException($"ffplay not found: {ffplayPath}")
            End If
            Dim protocol As String = If(String.IsNullOrEmpty(camera.Protocol), "udp", camera.Protocol)
            Dim ffplayProcess As New Process()
            ffplayProcess.StartInfo.FileName = ffplayPath
            ' Add additional parameters dynamically
            ffplayProcess.StartInfo.Arguments = $"-rtsp_transport {protocol} -i ""{ConstructRtspUrl(camera)}"" -window_title ""{camera.Name}"" -noborder {additionalArgs}"
            ffplayProcess.StartInfo.UseShellExecute = False
            ffplayProcess.StartInfo.RedirectStandardOutput = False
            ffplayProcess.StartInfo.CreateNoWindow = True
            ffplayProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden
            ffplayProcess.Start()
            ffplayProcesses.Add(ffplayProcess)
            WriteLog("info", $"Started ffplay process for camera '{camera.Name}' with PID {ffplayProcess.Id}")
            AnchorFFplayWindow(ffplayProcess, targetPanel, camera.Name)
        Catch ex As Exception
            WriteLog("error", $"Error starting ffplay for camera '{camera.Name}': {ex.Message}")
        End Try
    End Sub

    Private Sub AnchorFFplayWindow(ffplayProcess As Process, targetPanel As Panel, cameraName As String)
        Try
            WriteLog("info", $"Attempting to anchor ffplay window for camera '{cameraName}'")
            Dim handle As IntPtr = IntPtr.Zero
            Dim retries As Integer = 20
            Dim delay As Integer = 1000
            For attempt As Integer = 1 To retries
                handle = FindWindowByPartialTitle(cameraName)
                If handle <> IntPtr.Zero Then
                    WriteLog("info", $"Found ffplay window for camera '{cameraName}' on attempt {attempt}")
                    Exit For
                End If
                Threading.Thread.Sleep(delay)
            Next
            If handle = IntPtr.Zero Then
                Throw New Exception($"Failed to find ffplay window containing title '{cameraName}'.")
            End If
            ' Remove ffplay's window borders
            SetWindowLong(handle, GWL_STYLE, WS_VISIBLE)
            ' Set the parent to the target panel
            Dim parentResult = SetParent(handle, targetPanel.Handle)
            If parentResult = IntPtr.Zero Then
                WriteLog("error", $"SetParent failed for camera '{cameraName}' with error code {Marshal.GetLastWin32Error()}")
            Else
                WriteLog("debug", $"SetParent succeeded for camera '{cameraName}'")
            End If
            ' Allow ffplay to adjust before setting the position and size
            Threading.Thread.Sleep(500)
            Dim moveResult = MoveWindow(handle, 0, 0, targetPanel.Width, targetPanel.Height, True)
            If Not moveResult Then
                WriteLog("error", $"MoveWindow failed for camera '{cameraName}' with error code {Marshal.GetLastWin32Error()}")
            Else
                WriteLog("debug", $"MoveWindow succeeded for camera '{cameraName}'")
            End If

            WriteLog("info", $"Successfully anchored ffplay window for camera '{cameraName}'")
            WriteLog("debug", $"MoveWindow dimensions: {targetPanel.Width}x{targetPanel.Height}")
        Catch ex As Exception
            WriteLog("error", $"Error anchoring ffplay window for camera '{cameraName}': {ex.Message}")
        End Try
    End Sub

    Private Function FindWindowByPartialTitle(partialTitle As String) As IntPtr
        Dim foundHandle As IntPtr = IntPtr.Zero
        Dim allTitles As New List(Of String)
        WriteLog("debug", $"Looking for window title containing '{partialTitle}'.")
        EnumWindows(Function(hWnd, lParam)
                        Dim title As New System.Text.StringBuilder(256)
                        GetWindowText(hWnd, title, title.Capacity)
                        If title.Length > 0 Then
                            allTitles.Add(title.ToString().Trim())
                            If title.ToString().Trim() = partialTitle Then
                                foundHandle = hWnd
                                Return False
                            End If
                        End If
                        Return True
                    End Function, IntPtr.Zero)

        WriteLog("debug", $"Detected window titles: {String.Join(", ", allTitles)}")
        Return foundHandle
    End Function

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        For Each proc In ffplayProcesses
            If Not proc.HasExited Then
                proc.Kill()
            End If
        Next
        MyBase.OnFormClosing(e)
    End Sub
End Class


Public Class CameraTrayApp
    Inherits ApplicationContext
    Public ReadOnly InstallationPath As String = AppDomain.CurrentDomain.BaseDirectory
    Public Cameras As List(Of Object)
    Public SystemTrayIcon As NotifyIcon
    Public Sub New()
        Try
            LogUtility.WriteLog("info", "Initializing SystemTrayIcon.")

            ' Initialize tray icon and context menu
            SystemTrayIcon = New NotifyIcon() With { 
                .Icon = New Icon(System.IO.Path.Combine(InstallationPath, "CameraTrayApp.ico")), 
                .Text = "Camera Tray App", 
                .Visible = True 
            }
            LogUtility.WriteLog("debug", "Tray icon created successfully.")

            Dim contextMenu As New ContextMenuStrip()
            contextMenu.Items.Add("Show", Nothing, AddressOf btnShow)
            contextMenu.Items.Add("Configure", Nothing, AddressOf btnConfig)
            contextMenu.Items.Add("About", Nothing, AddressOf btnAbout)
            contextMenu.Items.Add("Exit", Nothing, AddressOf btnExit)
            SystemTrayIcon.ContextMenuStrip = contextMenu

            LogUtility.WriteLog("info", "Tray icon and context menu setup completed.")
        Catch ex As Exception
            LogUtility.WriteLog("error", $"Error during tray icon initialization: {ex.Message}")
            Throw
        End Try
    End Sub

    Public Sub btnAbout()
        Try
            LogUtility.WriteLog("info", "btnAbout invoked.")
            Dim About As New frmAbout()
            About.ShowDialog()
            LogUtility.WriteLog("info", "About dialog displayed successfully.")
        Catch ex As Exception
            LogUtility.WriteLog("error", "An error occurred while showing the About box: " & ex.Message)
            MessageBox.Show("An error occurred while opening the About box. Details have been logged.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Public Sub btnShow(Optional sender As Object = Nothing, Optional e As EventArgs = Nothing)
        Try
            LogUtility.WriteLog("info", "btnShow invoked.")
            Dim frmShow As New frmShow()
            frmShow.Show()
            LogUtility.WriteLog("info", "frmShow displayed successfully.")
        Catch ex As Exception
            LogUtility.WriteLog("error", "An error occurred while showing frmShow: " & ex.Message)
            MessageBox.Show("An error occurred while opening the Camera Feeds window. Details have been logged.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Public Sub btnConfig(Optional sender As Object = Nothing, Optional e As EventArgs = Nothing)
        LogUtility.WriteLog("info", "btnConfig invoked.")
        Dim frmConfig As New frmConfig()
        Dim result = frmConfig.ShowDialog()

        If result = DialogResult.OK Then
            LogUtility.WriteLog("info", "DialogResult is OK. Saving configuration.")
            ' Update and save configuration if the user clicks Save
            Dim config As New ConfigData With { .Cameras = frmConfig.UpdatedCameras.Cast(Of CameraConfig).ToList() }
            ConfigUtility.SaveConfig(config)
        Else
            LogUtility.WriteLog("info", "DialogResult is not OK. No changes saved.")
        End If

        LogUtility.WriteLog("info", "btnConfig completed.")
    End Sub

    Private Sub btnExit(sender As Object, e As EventArgs)
        LogUtility.WriteLog("info", "btnExit invoked. Application exiting.")
        SystemTrayIcon.Visible = False
        Application.Exit()
    End Sub

    

    ' Recursive helper function to ensure all levels of the directory exist
    Public Sub EnsureDirectoryRecursive(dir As String)
        If String.IsNullOrEmpty(dir) OrElse System.IO.Directory.Exists(dir) Then
            Return ' Base case: Stop if the directory exists or is invalid
        End If

        ' Recurse to ensure the parent directory exists
        Dim parentDirectory As String = System.IO.Path.GetDirectoryName(dir)
        EnsureDirectoryRecursive(parentDirectory)

        ' Create the current directory after parent is ensured
        System.IO.Directory.CreateDirectory(dir)
    End Sub

    Public Sub EnsureDirectoryExists(filePath As String)
        Try
            ' Extract the target directory
            Dim directory As String = System.IO.Path.GetDirectoryName(filePath)

            ' Ensure the target directory recursively
            EnsureDirectoryRecursive(directory)
        Catch ex As Exception
            ' Log or handle errors as needed
            Throw
        End Try
    End Sub

End Class

Public Class CameraConfig
    Public Property Name As String
    Public Property Url As String
    Public Property Protocol As String
    Public Property Username As String
    Public Property Password As String
    Public Property X As Integer
    Public Property Y As Integer
    Public Property Width As Integer
    Public Property Height As Integer
End Class

Public Class ConfigData
    Public Property Cameras As List(Of CameraConfig)
    Public Property debugLevel As String
End Class

Public Module ConfigUtility
    Public ReadOnly InstallPath As String = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cameras.config")
    Public ReadOnly AppDataPath As String = System.IO.Path.Combine(GetFolderPath(SpecialFolder.ApplicationData), Assembly.GetExecutingAssembly().GetCustomAttribute(Of AssemblyCompanyAttribute)?.Company.ToString(), "CameraTrayApp", "cameras.config")

    Public Function GetConfigFilePath() As String
        ' Check for config file in AppData first, then fallback to installation directory
        If File.Exists(AppDataPath) Then Return AppDataPath
        If File.Exists(InstallPath) Then Return InstallPath
        Return AppDataPath ' Default save location
    End Function



    Public Sub EnsureDirectoryExists(configFilePath As String)
        Try
            ' Extract the target directory and parent directory from configFilePath
            Dim directory As String = System.IO.Path.GetDirectoryName(configFilePath)
            Dim parentDirectory As String = System.IO.Path.GetDirectoryName(directory)

            ' Ensure the parent directory exists
            If Not String.IsNullOrEmpty(parentDirectory) AndAlso Not System.IO.Directory.Exists(parentDirectory) Then
                System.IO.Directory.CreateDirectory(parentDirectory)
            End If

            ' Ensure the target directory exists
            If Not System.IO.Directory.Exists(directory) Then
                System.IO.Directory.CreateDirectory(directory)
            End If
        Catch ex As Exception
            ' Log or handle errors as needed
            Throw
        End Try
    End Sub
   
    Private Function DefaultConfig() As ConfigData
        LogUtility.WriteLog("info", "Generating default configuration.")
        Return New ConfigData With {
            .Cameras = New List(Of CameraConfig)(),
            .debugLevel = "info"
        }
    End Function


    Public Sub SaveConfig(config As ConfigData)
        Try
            ' Get the path for the configuration file
            Dim configFilePath As String = GetConfigFilePath()
            LogUtility.WriteLog("info", $"Saving configuration to: {configFilePath}")

            ' Ensure the directory exists
            EnsureDirectoryExists(configFilePath)

            ' Serialize the configuration object to JSON
            Dim options As New JsonSerializerOptions With {.WriteIndented = True}
            Dim json As String = JsonSerializer.Serialize(config, options)

            ' Write the serialized JSON to the configuration file
            File.WriteAllText(configFilePath, json)
            LogUtility.WriteLog("info", $"Configuration saved successfully to: {configFilePath}")
        Catch ex As Exception
            LogUtility.WriteLog("error", $"Failed to save configuration: {ex.Message}")
            Throw
        End Try
    End Sub


    Public Function LoadConfig() As ConfigData
        Try
            Dim configFilePath As String = GetConfigFilePath()
            LogUtility.WriteLog("debug", $"Loading configuration file: '{configFilePath}'")

            ' Check if the configuration file exists
            If Not System.IO.File.Exists(configFilePath) Then
                LogUtility.WriteLog("warning", $"Configuration file not found at '{configFilePath}'. Returning default configuration.")
                Return DefaultConfig()
            End If

            ' Read and deserialize configuration
            Dim json As String = File.ReadAllText(configFilePath)
            LogUtility.WriteLog("debug", $"Configuration file read successfully. JSON content length: {json.Length}")

            Dim config As ConfigData = JsonSerializer.Deserialize(Of ConfigData)(json)
            If config Is Nothing Then
                Throw New Exception("Configuration deserialization returned null.")
            End If

            ' Ensure debugLevel is set, default to "info" if missing
            If String.IsNullOrEmpty(config.debugLevel) Then
                config.debugLevel = "info"
            End If

            LogUtility.WriteLog("info", $"Loaded configuration with {config.Cameras.Count} cameras and debug level '{config.debugLevel}'.")
            Return config
        Catch ex As JsonException
            LogUtility.WriteLog("error", $"JSON error while loading configuration: {ex.Message}")
            Return DefaultConfig()
        Catch ex As Exception
            LogUtility.WriteLog("error", $"Error loading configuration: {ex.Message}")
            Return DefaultConfig()
        End Try
    End Function


End Module