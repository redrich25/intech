Imports System.IO.Ports

Public Class Form1
    ' ONE serial port; do NOT drop a SerialPort component on the form.
    Private ReadOnly PortA As New SerialPort()

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' ---- Progress bars ----
        ' Ultrasonic distances (0..400 cm typical HC-SR04 range)
        ProgressBar1.Minimum = 0 : ProgressBar1.Maximum = 400 : ProgressBar1.Style = ProgressBarStyle.Continuous
        ProgressBar3.Minimum = 0 : ProgressBar3.Maximum = 400 : ProgressBar3.Style = ProgressBarStyle.Continuous

        ' pH 0..14 but we keep one decimal in the bar => 0..140 (×10)
        ProgressBar4.Minimum = 0 : ProgressBar4.Maximum = 140 : ProgressBar4.Style = ProgressBarStyle.Continuous

        ' ---- Serial ----
        With PortA
            .PortName = "COM13"          ' <-- set to your COM port
            .BaudRate = 115200
            .Parity = Parity.None
            .DataBits = 8
            .StopBits = StopBits.One
            .NewLine = vbLf              ' Arduino prints \r\n; ReadLine trims either
            AddHandler .DataReceived, AddressOf PortA_DataReceived
            .Open()
        End With

        feedlabel.Text = "Distance 1: -- cm"
        nutlabel.Text = "Distance 2: -- cm"
        phlabel.Text = "pH: --"
    End Sub

    ' ---- Serial line handler (NO Handles clause) ----
    Private Sub PortA_DataReceived(sender As Object, e As SerialDataReceivedEventArgs)
        Dim line As String
        Try
            line = PortA.ReadLine().Trim()
        Catch
            Exit Sub
        End Try

        ' Marshal to UI thread
        BeginInvoke(New Action(Sub()
                                   TextBox1.Text = line

                                   Dim vInt As Integer
                                   Dim vDbl As Double

                                   If line.StartsWith("DIST1=", StringComparison.OrdinalIgnoreCase) Then
                                       If Integer.TryParse(line.Substring(6).Trim(), vInt) Then
                                           UpdateDistance(ProgressBar1, feedlabel, vInt, "Distance 1")
                                       End If

                                   ElseIf line.StartsWith("DIST2=", StringComparison.OrdinalIgnoreCase) Then
                                       If Integer.TryParse(line.Substring(6).Trim(), vInt) Then
                                           UpdateDistance(ProgressBar3, nutlabel, vInt, "Distance 2")
                                       End If

                                   ElseIf line.StartsWith("PH=", StringComparison.OrdinalIgnoreCase) Then
                                       If Double.TryParse(line.Substring(3).Trim(),
                                                          Globalization.NumberStyles.Float,
                                                          Globalization.CultureInfo.InvariantCulture, vDbl) Then
                                           UpdatePH(ProgressBar4, phlabel, vDbl, "pH")
                                       End If

                                   Else
                                       ' Optional: naked number -> treat as DIST1
                                       If Integer.TryParse(line, vInt) Then
                                           UpdateDistance(ProgressBar1, feedlabel, vInt, "Distance 1")
                                       End If
                                   End If
                               End Sub))
    End Sub

    ' ---- UI helpers ----
    Private Sub UpdateDistance(pb As ProgressBar, lbl As Label, valueCm As Integer, caption As String)
        If pb Is Nothing OrElse lbl Is Nothing Then Exit Sub
        Dim v = Math.Max(pb.Minimum, Math.Min(pb.Maximum, valueCm))
        If pb.Value <> v Then pb.Value = v
        lbl.Text = $"{caption}: {v} cm"
    End Sub

    Private Sub UpdatePH(pb As ProgressBar, lbl As Label, ph As Double, caption As String)
        If pb Is Nothing OrElse lbl Is Nothing Then Exit Sub
        ' scale ×10 so we can show a decimal in an integer ProgressBar
        Dim scaled As Integer = CInt(Math.Round(ph * 10.0))
        scaled = Math.Max(pb.Minimum, Math.Min(pb.Maximum, scaled))
        If pb.Value <> scaled Then pb.Value = scaled
        lbl.Text = $"{caption}: {ph:F2}"
    End Sub

    ' ---- Clean shutdown ----
    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        Try
            RemoveHandler PortA.DataReceived, AddressOf PortA_DataReceived
            If PortA.IsOpen Then PortA.Close()
        Catch
        End Try
    End Sub

    ' ---- Buttons -> single chars to ESP32 ----
    ' Feed servo ON/OFF
    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        If PortA.IsOpen Then PortA.Write("a") ' feed open 90°
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        If PortA.IsOpen Then PortA.Write("b") ' feed closed 0°
    End Sub

    ' Nutrient servo ON/OFF
    Private Sub Button5_Click(sender As Object, e As EventArgs) Handles Button5.Click
        If PortA.IsOpen Then PortA.Write("c") ' nutrient open 90°
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        If PortA.IsOpen Then PortA.Write("d") ' nutrient closed 0°
    End Sub
End Class
