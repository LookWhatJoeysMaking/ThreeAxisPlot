' Three Axis Plot
' By Charles Jones, PMP, MCAD, et al...
' Dataman@Crjones.com
'
' Plots three inputs from arduino,
' As transmitted by the Three Axis Program.
' Red   X
' Green Y
' Blue  Z

Imports System.IO.Ports

' This is the main class
' Everything happens here
Public Class ThreeAxisPlot

    ' Pulic Variables
    Dim mPort As SerialPort      ' This is the Serial Port we are talking to
    Dim mCount As Integer = 0    ' This is the input field number
    Dim mSync As Integer = 0     ' This is a flag to find the start of the data stream
    Dim mByte As Byte            ' Temporary storage for the next income character
    Dim mInt As String = ""      ' Temporary storage for the incoming number, built character by character.
    Dim X As Integer             ' Storage for X
    Dim Y As Integer             ' Storage for Y
    Dim Z As Integer             ' Storage for Z
    Dim mBitMap As Bitmap        ' Store for the Graph Display
    Dim g As Graphics            ' Allows us to draw on Graph
    Dim x1 As Integer = 0        ' Remembers last x
    Dim y1 As Integer = 0        ' Remembers last y
    Dim z1 As Integer = 0        ' Remembers last z
    Dim ty As Double = 0         ' Total Y height for graph

    ' Loaded: Performs actions when the form is first started.
    ' Like Arduino Setup Function, only run once.
    Private Sub Loaded(sender As Object, e As System.EventArgs) Handles Me.Load
        ' Add list of Com Ports to Combo Box
        For Each port As String In SerialPort.GetPortNames()
            cbPorts.Items.Add(port)
        Next port

        ' Select last item in each combo box by default
        Try
            cbBaud.SelectedIndex = cbBaud.Items.Count() - 1
            cbPorts.SelectedIndex = cbPorts.Items.Count() - 1
        Catch ex As Exception
        End Try

        ' Set up the Graph
        mBitMap = New Bitmap(pbPlot.Width, pbPlot.Height)
        g = Graphics.FromImage(mBitMap)
        g.FillRectangle(Brushes.White, New Rectangle(0, 0, pbPlot.Width, pbPlot.Height))
        pbPlot.Image = mBitMap

        ty = pbPlot.Height - 2

    End Sub

    ' btnOpen: Happens when the button is clicked
    Private Sub btnOpen_Click(sender As System.Object, e As System.EventArgs) Handles btnOpen.Click

        ' We use the text on the button face to keep status,
        ' Whether we should open or close the port.

        ' If the button say OPEN, then open the port
        If btnOpen.Text = "Open" Then
            ' Set button text to CLOSE
            btnOpen.Text = "Close"
            Application.DoEvents()
            ' Open the port
            mPort = New SerialPort(cbPorts.SelectedItem, cbBaud.SelectedItem)
            mPort.Open()
            ' Start the Timer
            Timer1.Interval = 100
            Timer1.Enabled = True
        Else
            ' Otherwise: button face must read CLOSE
            ' So, try to disable the timer and port
            Try
                Timer1.Enabled = False
                mPort.Close()
            Catch ex As Exception
            End Try
            ' Set button text back to OPEN so we can reopen if needed.
            btnOpen.Text = "Open"
        End If
    End Sub

    ' These are pens used to draw the graph.
    ' We only define these once, as globals.
    ' Faster than defining then each time.
    Dim penX As New System.Drawing.Pen(Color.Red, 1)
    Dim penY As New System.Drawing.Pen(Color.Green, 1)
    Dim penZ As New System.Drawing.Pen(Color.Blue, 1)


    ' Plot: Plots the graph
    Private Sub Plot()

        ' Copy the ride side of the Bitmap object.
        Dim cloneRect As New Rectangle(1, 0, pbPlot.Width - 1, pbPlot.Height)
        Dim cloneBitmap As Bitmap = mBitMap.Clone(cloneRect, mBitMap.PixelFormat)

        ' Blank the bitmap
        g.FillRectangle(Brushes.White, New Rectangle(0, 0, pbPlot.Width, pbPlot.Height))

        ' And shift orignal image left by 1 pixel
        g.DrawImage(cloneBitmap, 0, 0)

        ' Now draw each of X, Y, and Z
        x1 = DrawPoint(penX, g, X, x1)
        y1 = DrawPoint(penY, g, Y, y1)
        z1 = DrawPoint(penZ, g, Z, z1)

        ' Update the image
        pbPlot.Image = mBitMap
        pbPlot.Refresh()

        ' Display what we read for debugging purposes
        TextBox1.Text = X.ToString() + " " & Y.ToString() & " " & Z.ToString()
        Application.DoEvents()
    End Sub

    ' DrawPoint: This does the actual work of drawing the pixel
    Private Function DrawPoint(ByRef p As Pen, ByRef g As Graphics, ByVal i As Integer, ByVal iprev As Integer) As Integer
        ' First, we need to scale the point to the current graphic size
         Dim oy As Double = ty * (CDbl(i) / CDbl(1024))
        ' Then flip it, as windows are based on 0,0
        Dim yy As Integer = (ty - CInt(oy))
        ' Finally plot it
        g.DrawLine(p, pbPlot.Width - 2, iprev, pbPlot.Width - 1, yy)
        ' g.DrawRectangle(p, pbPlot.Width - 1, yy, 1, 1)
        Return yy
    End Function


    ' Timer1_Tick: Timer Servicing Routine
    ' This happens every once a second.
    ' Read the incoming characters...
    ' Data will be in the format:
    ' 123 254 124 CR LF
    ' X   Y   Z   END OF LINE
    ' WHERE CR is 13
    ' AND   LF is 10
    ' So, each line starts with chr(10)
    ' With spaces between the numbers
    Private Sub Timer1_Tick(sender As System.Object, e As System.EventArgs) Handles Timer1.Tick
        ' If we have something to read...
        While (mPort.BytesToRead)
            ' Read the byte
            mByte = mPort.ReadByte()
            '  If we haven't sync'd to the start of the stream yet,
            ' Trap all input till we do.
            If mSync < 1 Then
                mCount = 0
                If mByte <> 10 Then
                    mSync = 0
                Else
                    mSync = 1
                    mInt = ""
                End If
                Continue While
            End If
            ' Otherwise we did sync to the start of the stream,
            ' So now we are reading the input
            '
            ' if there's a space or end of line...
            If (mByte = 32 Or mByte = 13) Then
                mCount += 1
                Try
                    ' Store the Data in the appropriate variable x, y, or z
                    Select Case mCount
                        Case 1 : X = Int16.Parse(mInt)
                        Case 2 : Y = Int16.Parse(mInt)
                        Case 3 : Z = Int16.Parse(mInt)
                    End Select
                Catch
                End Try
                mInt = ""
            ElseIf (mByte = 10) Then
                ' If end of line, then plot results
                Plot()
                mCount = 0
            Else
                ' Otherwise, we are inside the line, store the current character to the temp integer
                mInt &= Chr(mByte)
            End If
        End While
        ' All out of input to read, update the screen and exit
        Application.DoEvents()
    End Sub


End Class
