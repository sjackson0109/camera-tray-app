Imports System.IO
Imports System.Diagnostics
Imports System.Drawing
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports System.Text.Json

Imports System.Windows.Forms 
 
Module Program
    Sub Main(args As String())
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)

        If args.Length > 0 Then
            Select Case args(0).ToLower()
                Case "about"
                    Dim trayApp As New CameraTrayApp()
                    trayApp.About()
                Case "show"
                    Dim trayApp As New CameraTrayApp()
                    trayApp.Show()
                Case "config"
                    Dim trayApp As New CameraTrayApp()
                    trayApp.Configure()
                Case Else
                    MessageBox.Show("Unknown argument. Valid options: 'show'.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Select
        Else
            Application.Run(New CameraTrayApp())
        End If
    End Sub
End Module

Public Module LogUtility
    Private ReadOnly LogDirectory As String = InitializeLogDirectory()

    Private Function InitializeLogDirectory() As String
        Dim directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "\", _
        Assembly.GetExecutingAssembly().GetCustomAttribute(Of AssemblyCompanyAttribute)?.Company.ToString(), "\", _
        Assembly.GetExecutingAssembly().GetCustomAttribute(Of AssemblyProductAttribute)?.Product.ToString(), "\logs")

        ' Ensure the directory exists
        EnsureDirectoryExists(directory)

        Return directory
    End Function

    Private Sub EnsureDirectoryExists(directoryPath As String)
        If Not Directory.Exists(directoryPath) Then
            Directory.CreateDirectory(directoryPath)
        End If
    End Sub

    Public Sub WriteLog(logType As String, message As String, Optional ex As Exception = Nothing)
        Try
            ' Construct the log file path and entry
            Dim logFilePath As String = Path.Combine(LogDirectory, $"{logType}.log")
            Dim timestamp As String = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}]"
            Dim logEntry As String = $"{timestamp} [{logType.ToUpper()}] {message}"

            ' Append exception details if provided
            If ex IsNot Nothing Then
                logEntry &= $"{Environment.NewLine}Exception: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}{Environment.NewLine}"
            End If

            ' Append the log entry to the runtime log file
            File.AppendAllText(logFilePath, logEntry & Environment.NewLine)
        Catch writeEx As Exception
            ' Fallback mechanism for logging errors
            Dim fallbackLogPath = Path.Combine(LogDirectory, "fallback_error.log")
            Dim fallbackLogEntry As String = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [ERROR] Failed to write log entry: {writeEx.Message}{Environment.NewLine}"
            File.AppendAllText(fallbackLogPath, fallbackLogEntry)
        End Try
    End Sub
End Module

Public Class AboutBox
    Inherits Form

    Private memoField As TextBox
    Private authorLink As LinkLabel
    Private versionLabel As Label
    Private hyperlink As LinkLabel

    Public Sub New()
        ' Set form properties
        Me.Text = "About"
        Me.Size = New Size(400, 300)
        Me.StartPosition = FormStartPosition.CenterScreen

        ' Create and configure memo field
        memoField = New TextBox()
        memoField.Multiline = True
        memoField.ReadOnly = True
        memoField.ScrollBars = ScrollBars.Vertical
        memoField.Size = New Size(250, 120)
        memoField.Location = New Point(20, 20)
        memoField.Text = "A lightweight system tray application for managing and viewing RTSP camera feeds."
        Me.Controls.Add(memoField)

        ' Create and configure author link
        authorLabel = New Label()
        authorLabel.Text = "Author: " & Assembly.GetExecutingAssembly().GetCustomAttribute(Of AssemblyCompanyAttribute)()?.Company.ToString()
        authorLabel.Location = New Point(20, 140)
        authorLabel.AutoSize = True
        Me.Controls.Add(authorLabel)

        ' Create and configure version label
        Dim version = Assembly.GetExecutingAssembly().GetName().Version.ToString()
        versionLabel = New Label()
        versionLabel.Text = "Version: " & version
        versionLabel.Location = New Point(20, 170)
        versionLabel.AutoSize = True
        Me.Controls.Add(versionLabel)

        ' Create and configure hyperlink
        hyperlink = New LinkLabel()
        hyperlink.Text = "source code"
        hyperlink.Location = New Point(20, 200)
        hyperlink.AutoSize = True
        AddHandler hyperlink.LinkClicked, AddressOf HyperlinkClicked
        Me.Controls.Add(hyperlink)
    End Sub

    Private Sub HyperlinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs)
        Process.Start(New ProcessStartInfo("https://www.github.com/sjackson0109/camera-tray-app") With {
            .UseShellExecute = True
        })
    End Sub

End Class


Public Class CameraTrayApp
    Inherits ApplicationContext

    Public ReadOnly InstallationPath As String = AppDomain.CurrentDomain.BaseDirectory
    Public ReadOnly UserAppPath As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "\", _
        Assembly.GetExecutingAssembly().GetCustomAttribute(Of AssemblyCompanyAttribute)?.Company.ToString(), "\", _
        Assembly.GetExecutingAssembly().GetCustomAttribute(Of AssemblyProductAttribute)?.Product.ToString())
    Public ReadOnly UserLogPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "\logs")

    Public ReadOnly DefaultConfigFile As String = Path.Combine(InstallationPath, "\", "camera.config")
    Public ReadOnly UserConfigFile As String = Path.Combine(UserAppPath, "\", "camera.config")
    Public ReadOnly UserLogFile As String = Path.Combine(UserLogPath, "runtime.log")
    Public Cameras As List(Of Config)
    Public trayIcon As NotifyIcon

    Public Sub New()
        ' Initialize tray icon and context menu
        trayIcon = New NotifyIcon() With {
            .Icon = New Icon(Path.Combine(InstallationPath, "CameraTrayApp.ico")),
            .Visible = True,
            .Text = "Camera Tray App"
        }

        Dim contextMenu As New ContextMenuStrip()
        contextMenu.Items.Add("Show Cameras", Nothing, AddressOf Show)
        contextMenu.Items.Add("Configure", Nothing, AddressOf Configure)
        contextMenu.Items.Add("About", Nothing, AddressOf About)
        contextMenu.Items.Add("Exit", Nothing, AddressOf ExitApplication)

        trayIcon.ContextMenuStrip = contextMenu

        ' Load configuration on initialization
        Load()
    End Sub

    Public Sub About()
        Try
            Dim aboutBox As New AboutBox()
            aboutBox.ShowDialog()
        Catch ex As Exception
            WriteLog("error", "An error occurred while showing the About box.", ex)
            MessageBox.Show("An error occurred while opening the About box. Details have been logged.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub


    Public Sub Show(Optional sender As Object = Nothing, Optional e As EventArgs = Nothing)
        Dim cameras = Load()
        Dim cameraGridForm As New CameraGridForm(cameras)
        cameraGridForm.Show()
    End Sub

    Public Sub Configure(Optional sender As Object = Nothing, Optional e As EventArgs = Nothing)
        Dim configureForm As New ConfigureForm(Cameras)
        Dim result = configureForm.ShowDialog()

        If result = DialogResult.OK Then
            ' Update and save configuration if the user clicks Save
            cameras = configureForm.UpdatedCameras
            Save()
        End If
    End Sub

    Private Sub EnsureDirectoryExists(directoryPath As String)
        If Not System.IO.Directory.Exists(directoryPath) Then
            System.IO.Directory.CreateDirectory(directoryPath)
        End If
    End Sub

    Private Sub ExitApplication(sender As Object, e As EventArgs)
        trayIcon.Visible = False
        Application.Exit()
    End Sub

    ' Load the configuration from file
    Private Function Load() As List(Of Config)
        Cameras = New List(Of Config)()
        Try
            Dim configFilePath As String = If(File.Exists(UserAppPath & "cameras.config"), UserAppPath & "cameras.config", InstallationPath & "cameras.config")
            ' Read and parse the configuration file
            Dim json As String = File.ReadAllText(configFilePath)
            Dim options As New JsonSerializerOptions With {.PropertyNameCaseInsensitive = True}
            Cameras = JsonSerializer.Deserialize(Of List(Of Config))(json, options)

            WriteLog("info", $"Loaded {Cameras.Count} cameras successfully from '{configFilePath}'.")
        Catch ex As Exception
            ' Log and notify the user of any errors
            Dim configFilePath As String = If(File.Exists(UserAppPath & "cameras.config"), UserAppPath & "cameras.config", InstallationPath & "cameras.config")
            WriteLog("error", $"Error loading configuration from '{configFilePath}'.", ex)
            MessageBox.Show($"Error loading configuration. Details have been logged.", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try

        Return Cameras
    End Function

    ' Save the configuration to file
    Private Sub Save()
        Try
            EnsureDirectoryExists(UserAppPath) ' Create the directory if necessary
            Dim configFilePath As String = If(File.Exists(UserAppPath & "cameras.config"), UserAppPath & "cameras.config", InstallationPath & "cameras.config")

            Dim options As New JsonSerializerOptions With {.WriteIndented = True}
            Dim updatedConfig As New With {.Cameras = Cameras}
            File.WriteAllText(configFilePath, JsonSerializer.Serialize(updatedConfig, options))

            WriteLog("info", $"Configuration successfully saved to '{configFilePath}'.")
        Catch ex As Exception
            Dim configFilePath As String = If(File.Exists(UserAppPath & "cameras.config"), UserAppPath & "cameras.config", InstallationPath & "cameras.config")
            WriteLog("error", $"Error saving configuration to '{configFilePath}'.", ex)
            MessageBox.Show($"Error saving configuration. Details have been logged.", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
End Class

Public Class ConfigureForm
    Inherits Form

    Public Property UpdatedCameras As List(Of Config)

    Private Shadows saveButton As Button
    Private Shadows cancelButton As Button
    Private camerasTable As DataGridView
    Private memo As Label

    Public Sub New(cameras As List(Of Config))
        If cameras Is Nothing Then
            Throw New ArgumentNullException(NameOf(cameras), "Cameras list cannot be null.")
        End If

        ' Initialize form
        WriteLog("info", "Configure Cameras form opened")
        Me.Text = "Configure Cameras"
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.FixedDialog ' Make the form non-resizable

        ' Create Memo Label
        memo = New Label() With {
            .Text = "Please input the following information for each camera:" & Environment.NewLine &
                    "- Name: A descriptive name for the camera." & Environment.NewLine &
                    "- URL: The RTSP URL for the camera feed." & Environment.NewLine &
                    "- Username: (Optional) RTSP username for the camera feed." & Environment.NewLine &
                    "- Password: (Optional) RTSP password for the camera feed." & Environment.NewLine &
                    "- X, Y: Coordinates for the camera feed placement." & Environment.NewLine &
                    "- Width, Height: Dimensions of the camera feed window.",
            .Dock = DockStyle.Top,
            .AutoSize = True,
            .Padding = New Padding(10)
        }
        Me.Controls.Add(memo)

        ' Create DataGridView
        camerasTable = New DataGridView() With {
            .Dock = DockStyle.Top,
            .AllowUserToAddRows = True,
            .AllowUserToDeleteRows = True,
            .AutoGenerateColumns = False
        }

        ' Add columns for the camera configuration
        camerasTable.Columns.Add(New DataGridViewTextBoxColumn() With {
            .HeaderText = "Name",
            .DataPropertyName = "Name",
            .Width = 120
        })
        camerasTable.Columns.Add(New DataGridViewTextBoxColumn() With {
            .HeaderText = "URL",
            .DataPropertyName = "Url",
            .Width = 350
        })
        camerasTable.Columns.Add(New DataGridViewTextBoxColumn() With {
            .HeaderText = "Username",
            .DataPropertyName = "Username",
            .Width = 80
        })
        camerasTable.Columns.Add(New DataGridViewTextBoxColumn() With {
            .HeaderText = "Password",
            .DataPropertyName = "Password",
            .Width = 80
        })
        camerasTable.Columns.Add(New DataGridViewTextBoxColumn() With {
            .HeaderText = "X",
            .DataPropertyName = "X",
            .Width = 30
        })
        camerasTable.Columns.Add(New DataGridViewTextBoxColumn() With {
            .HeaderText = "Y",
            .DataPropertyName = "Y",
            .Width = 30
        })
        camerasTable.Columns.Add(New DataGridViewTextBoxColumn() With {
            .HeaderText = "Width",
            .DataPropertyName = "Width",
            .Width = 45
        })
        camerasTable.Columns.Add(New DataGridViewTextBoxColumn() With {
            .HeaderText = "Height",
            .DataPropertyName = "Height",
            .Width = 50
        })

        ' Bind cameras to the DataGridView
        camerasTable.DataSource = New BindingSource() With {
            .DataSource = cameras
        }
        Me.Controls.Add(camerasTable)

        ' Create Save Button
        saveButton = New Button() With {
            .Text = "Save",
            .Dock = DockStyle.Bottom
        }
        AddHandler saveButton.Click, AddressOf OnSave
        Me.Controls.Add(saveButton)

        ' Create Cancel Button
        cancelButton = New Button() With {
            .Text = "Cancel",
            .Dock = DockStyle.Bottom
        }
        AddHandler cancelButton.Click, AddressOf OnCancel
        Me.Controls.Add(cancelButton)

        ' Set form dimensions
        AdjustFormSize(camerasTable, memo)

        ' Clone the cameras for editing
        Me.UpdatedCameras = cameras.Select(Function(c) New Config() With {
            .Name = c.Name,
            .Url = c.Url,
            .Username = c.Username,
            .Password = c.Password,
            .X = If(c.X, 600),
            .Y = If(c.Y, 600),
            .Width = If(c.Width, 640),
            .Height = If(c.Height, 480)
        }).ToList()
    End Sub

    Private Sub AdjustFormSize(dataGrid As DataGridView, headerLabel As Label)
        ' Calculate the total column widths
        Dim totalColumnWidth As Integer = dataGrid.Columns.Cast(Of DataGridViewColumn)().Sum(Function(col) col.Width)
        Dim actionColumnWidth As Integer = 20 ' Width for the action column on the left
        Dim padding As Integer = 40 ' Extra space for borders and scrollbars
        Dim estimatedRowHeight As Integer = 24 ' Approximate height for each row
        Dim rowCount As Integer = Math.Max(dataGrid.Rows.Count, 5) ' Ensure a minimum of 5 rows are displayed

        ' Set form dimensions
        Me.Width = totalColumnWidth + actionColumnWidth + padding
        Me.Height = headerLabel.Height + (rowCount * estimatedRowHeight) + saveButton.Height + cancelButton.Height + 80
        dataGrid.Height = rowCount * estimatedRowHeight
    End Sub

    Private Sub OnSave(sender As Object, e As EventArgs)
        ' Commit changes and close the form
        Me.UpdatedCameras = CType(camerasTable.DataSource, BindingSource).List.Cast(Of Config).ToList()
        Me.DialogResult = DialogResult.OK ' Mark the form result as "OK"
        WriteLog("info", "Camera Configuration SAVED")
        Me.Close()
    End Sub

    Private Sub OnCancel(sender As Object, e As EventArgs)
        ' Discard changes and close the form
        Me.DialogResult = DialogResult.Cancel ' Mark the form result as "Cancel"
        WriteLog("info", "Camera Configuration CANCELLED")
        Me.Close()
    End Sub

End Class

Public Class CameraGridForm
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

    Private ReadOnly Cameras As List(Of Config)
    Private ffplayProcesses As New List(Of Process)()

    Public Sub New(cameras As List(Of Config))
        Try
            WriteLog("info", "Initializing CameraGridForm.")

            Me.Cameras = cameras
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
            WriteLog("info", "CameraGridForm initialized successfully.")
        Catch ex As Exception
            WriteLog("error", $"Error initializing CameraGridForm: {ex.Message}", ex)
            Throw
        End Try
    End Sub

    Private Sub SetupGrid()
        Try
            WriteLog("info", "Setting up grid.")

            ' Clear previous controls
            Me.Controls.Clear()

            Dim columns As Integer = If(Cameras.Count = 1, 1, Math.Max(1, CInt(Math.Ceiling(Math.Sqrt(Cameras.Count)))))
            Dim rows As Integer = If(Cameras.Count = 1, 1, CInt(Math.Ceiling(Cameras.Count / columns)))

            Dim panelWidth As Integer = Me.ClientSize.Width \ columns
            Dim panelHeight As Integer = Me.ClientSize.Height \ rows

            For i As Integer = 0 To Cameras.Count - 1
                Dim camera = Cameras(i)
                Dim panel As New Panel With {
                    .Width = panelWidth,
                    .Height = panelHeight,
                    .Left = (i Mod columns) * panelWidth,
                    .Top = (i \ columns) * panelHeight,
                    .BackColor = Color.Black ' Set default background
                }
                Me.Controls.Add(panel)

                Dim rtspUrl As String = ConstructRtspUrl(camera)
                StartFFplay(rtspUrl, panel, camera.Name)
            Next

            WriteLog("info", "SetupGrid completed successfully.")
        Catch ex As Exception
            WriteLog("error", $"Error in SetupGrid: {ex.Message}", ex)
            Throw
        End Try
    End Sub

    Private Sub OnFormResize(sender As Object, e As EventArgs)
        Try
            WriteLog("info", "Form resized. Adjusting grid.")

            Dim columns As Integer = If(Cameras.Count = 1, 1, Math.Max(1, CInt(Math.Ceiling(Math.Sqrt(Cameras.Count)))))
            Dim rows As Integer = If(Cameras.Count = 1, 1, CInt(Math.Ceiling(Cameras.Count / columns)))

            Dim panelWidth As Integer = Me.ClientSize.Width \ columns
            Dim panelHeight As Integer = Me.ClientSize.Height \ rows

            For i As Integer = 0 To Cameras.Count - 1
                Dim panel = Me.Controls(i)
                If TypeOf panel Is Panel Then
                    panel.Width = panelWidth
                    panel.Height = panelHeight
                    panel.Left = (i Mod columns) * panelWidth
                    panel.Top = (i \ columns) * panelHeight

                    Dim handle = FindWindowByPartialTitle(Cameras(i).Name)
                    If handle <> IntPtr.Zero Then
                        MoveWindow(handle, 0, 0, panel.Width, panel.Height, True)
                    End If
                End If
            Next
        Catch ex As Exception
            WriteLog("error", $"Error resizing grid: {ex.Message}", ex)
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

    Private Function ConstructRtspUrl(camera As Config) As String
        Dim uri As New Uri(camera.Url)
        Dim port As Integer = If(uri.IsDefaultPort, 554, uri.Port)
        Return $"{uri.Scheme}://{camera.Username}:{camera.Password}@{uri.Host}:{port}{uri.PathAndQuery}"
    End Function

    Private Sub StartFFplay(rtspUrl As String, targetPanel As Panel, cameraName As String)
        Try
            Dim ffplayPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lib", "ffplay.exe")

            ' Verify if ffplay.exe exists
            If Not File.Exists(ffplayPath) Then
                WriteLog("error", $"ffplay executable not found at path '{ffplayPath}'")
                Throw New FileNotFoundException($"ffplay not found: {ffplayPath}")
            End If

            Dim ffplayProcess As New Process()
            ffplayProcess.StartInfo.FileName = ffplayPath
            ffplayProcess.StartInfo.Arguments = $"-rtsp_transport tcp -i ""{rtspUrl}"" -window_title ""{cameraName}"" -noborder"
            ffplayProcess.StartInfo.UseShellExecute = False
            ffplayProcess.StartInfo.RedirectStandardOutput = False
            ffplayProcess.StartInfo.CreateNoWindow = True
            ffplayProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden

            ffplayProcess.Start()
            ffplayProcesses.Add(ffplayProcess)

            WriteLog("info", $"Started ffplay process for camera '{cameraName}' with PID {ffplayProcess.Id}")
            AnchorFFplayWindow(ffplayProcess, targetPanel, cameraName)
        Catch ex As Exception
            WriteLog("error", $"Error starting ffplay for camera '{cameraName}': {ex.Message}", ex)
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
                'WriteLog("debug", $"Retry {attempt}/{retries}: No matching window found yet for camera '{cameraName}'")
                Threading.Thread.Sleep(delay)
            Next

            If handle = IntPtr.Zero Then
                Throw New Exception($"Failed to find ffplay window containing title '{cameraName}'.")
            End If

            ' Remove ffplay's window borders
            SetWindowLong(handle, GWL_STYLE, WS_VISIBLE)

            ' Set the parent to the target panel
            'SetParent(handle, targetPanel.Handle)
            Dim parentResult = SetParent(handle, targetPanel.Handle)
            If parentResult = IntPtr.Zero Then
                WriteLog("error", $"SetParent failed for camera '{cameraName}' with error code {Marshal.GetLastWin32Error()}")
            Else
                WriteLog("debug", $"SetParent succeeded for camera '{cameraName}'")
            End If

            ' Allow ffplay to adjust before setting the position and size
            Threading.Thread.Sleep(500)

            'MoveWindow(handle, 0, 0, targetPanel.Width, targetPanel.Height, True)
            Dim moveResult = MoveWindow(handle, 0, 0, targetPanel.Width, targetPanel.Height, True)
            If Not moveResult Then
                WriteLog("error", $"MoveWindow failed for camera '{cameraName}' with error code {Marshal.GetLastWin32Error()}")
            Else
                WriteLog("debug", $"MoveWindow succeeded for camera '{cameraName}'")
            End If

            WriteLog("info", $"Successfully anchored ffplay window for camera '{cameraName}'")

            ' Log debug information for MoveWindow dimensions
            WriteLog("debug", $"MoveWindow dimensions: {targetPanel.Width}x{targetPanel.Height}")
        Catch ex As Exception
            WriteLog("error", $"Error anchoring ffplay window for camera '{cameraName}': {ex.Message}", ex)
        End Try
    End Sub


    Private Function FindWindowByPartialTitle(partialTitle As String) As IntPtr
        Dim foundHandle As IntPtr = IntPtr.Zero
        Dim allTitles As New List(Of String)

        WriteLog("debug", $"Looking for window title containing '{partialTitle}'. Detected window titles: {String.Join(", ", allTitles)}")

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

        ' Log all detected window titles
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

Public Class TrayIconManager
    Private trayIcon As NotifyIcon
    Private cameraApp As CameraTrayApp

    Public Sub New()
        cameraApp = New CameraTrayApp()

        trayIcon = New NotifyIcon() With {
            .Icon = New Icon("CameraTrayApp.ico"),
            .Visible = True,
            .Text = Assembly.GetExecutingAssembly().GetCustomAttribute(Of AssemblyTitleAttribute)()?.Title.ToString()
        }
    End Sub

End Class

Public Class Config
    Public Property Name As String
    Public Property Url As String
    Public Property Username As String
    Public Property Password As String
    Public Property X As Integer?
    Public Property Y As Integer?
    Public Property Width As Integer?
    Public Property Height As Integer?

    Public Sub New()
    End Sub

    Public Sub New(name As String, url As String, username As String, password As String, x As Integer?, y As Integer?, width As Integer?, height As Integer?)
        Me.Name = name
        Me.Url = url
        Me.Username = username
        Me.Password = password
        Me.X = x
        Me.Y = y
        Me.Width = width
        Me.Height = height
    End Sub
End Class
