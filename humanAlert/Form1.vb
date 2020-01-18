Imports System.Net.NetworkInformation
Public Class Form1
    Public Declare Sub keybd_event Lib "user32" (ByVal bVk As Byte, ByVal bScan As Byte, ByVal dwFlags As Integer, ByVal dwExtraInfo As Integer)

    Dim SENSOR_IP As String
    Dim LAST_HEALTHCHECK_TIME As DateTime
    Dim SOCK As New System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp)

    'クライアントの接続待ちスタート
    Private Sub StartAccept(ByVal server As System.Net.Sockets.Socket)
        '接続要求待機を開始する
        server.BeginAccept(New System.AsyncCallback(AddressOf AcceptCallback), server)
    End Sub

    'BeginAcceptのコールバック
    Private Sub AcceptCallback(ByVal ar As System.IAsyncResult)
        'サーバーSocketの取得
        Dim server As System.Net.Sockets.Socket = CType(ar.AsyncState, System.Net.Sockets.Socket)

        '接続要求を受け入れる
        Dim client As System.Net.Sockets.Socket = Nothing
        Try
            'クライアントSocketの取得
            client = server.EndAccept(ar)
        Catch
            System.Console.WriteLine("閉じました。")
            Return
        End Try
        Dim resBytes(2047) As Byte
        Dim resSize As Integer = client.Receive(resBytes, resBytes.Length, System.Net.Sockets.SocketFlags.None)
        Dim resString As String = System.Text.Encoding.UTF8.GetString(resBytes, 0, resSize)
        Console.WriteLine("resdata={0}", resString)
        Dim client_ip As System.Net.IPEndPoint = client.RemoteEndPoint
        Console.WriteLine("connect from ={0}", client_ip.Address)
        SENSOR_IP = client_ip.Address.ToString
        Invoke(New SetStatusLabel_delegate(AddressOf SetSensorIPLabel), SENSOR_IP)
        Invoke(New SetStatusLabel_delegate(AddressOf SetStatusLabel), "センサー接続済み")


        If resString = "ping" Then
            client.Send(System.Text.Encoding.UTF8.GetBytes("pong"))
            LAST_HEALTHCHECK_TIME = DateTime.Now
            Invoke(New SetStatusLabel_delegate(AddressOf SetLastHealthCheckTimeLabel), LAST_HEALTHCHECK_TIME.ToLongTimeString)
        ElseIf resString = "alert" Then
            client.Send(System.Text.Encoding.UTF8.GetBytes("OK"))
            AlertFunctionDlg()
        End If

        client.Shutdown(System.Net.Sockets.SocketShutdown.Both)
        client.Close()

        '接続要求待機を再開する
        server.BeginAccept(New System.AsyncCallback(AddressOf AcceptCallback), server)
    End Sub

    Private Sub AlertFunctionDlg()
        Invoke(New SetStatusLabel_delegate(AddressOf SetStatusLabel), "アラート受信")
        'System.Threading.Thread.Sleep(300)
        Call keybd_event(173, 0, 0, 0) 'mute
        Call keybd_event(173, 0, 2, 0)
        System.Threading.Thread.Sleep(100)
        Call keybd_event(&H5B, 0, 0, 0) 'Windowsキーを押す
        Call keybd_event(77, 0, 0, 0) 'm
        Call keybd_event(77, 0, 2, 0)
        Call keybd_event(&H5B, 0, 2, 0)

        NotifyIcon1.BalloonTipText = "検知"
        NotifyIcon1.BalloonTipIcon = ToolTipIcon.Error
        NotifyIcon1.ShowBalloonTip(3000)
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        SetStatusLabel("接続待ち")
        Button1.Enabled = False
        Button2.Enabled = True
        Timer1.Enabled = True
        Timer1.Start()
        SendBroadcast()
        Dim localIpString As String = "0.0.0.0"
        Dim localAddress As System.Net.IPAddress = System.Net.IPAddress.Parse(localIpString)
        Dim localPort As Integer = 8000

        'UdpClientを作成し、ローカルエンドポイントにバインドする
        Dim localEP As New System.Net.IPEndPoint(localAddress, localPort)
        SOCK.Bind(localEP)
        SOCK.Listen(100)
        StartAccept(SOCK)

    End Sub

    Delegate Sub SetStatusLabel_delegate(str As String)

    Private Sub SetSensorIPLabel(str As String)
        Label4.Text = str
    End Sub

    Private Sub SetStatusLabel(str As String)
        Label1.Text = str
    End Sub

    Private Sub SetLastHealthCheckTimeLabel(str As String)
        Label6.Text = str
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Timer1.Enabled = False
        Timer1.Stop()

        SOCK.Close()
        SOCK = New System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp)
        SetStatusLabel("待機中")
        SENSOR_IP = ""
        SetSensorIPLabel("未接続")
        Button1.Enabled = True
        Button2.Enabled = False
    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        If Not SENSOR_IP = "" Then
            Dim delta As Int64 = (DateTime.Now - LAST_HEALTHCHECK_TIME).Ticks
            Console.WriteLine(delta / 1000 / 1000 / 10)
            If delta / 1000 / 1000 / 10 > 10 Then
                SENSOR_IP = ""
                SetSensorIPLabel("未接続")
                SetStatusLabel("タイムアウト")
                NotifyIcon1.BalloonTipText = "センサータイムアウト"
                NotifyIcon1.BalloonTipIcon = ToolTipIcon.Error
                NotifyIcon1.ShowBalloonTip(5000)
            End If
        Else
            SendBroadcast()
        End If
    End Sub


    Private Sub SendBroadcast()
        'データを送信するリモートホストとポート番号
        Dim remoteHost As String = getCurrentBroadcastAddr() '"192.168.15.255"
        Dim remotePort As Integer = 8000
        'UdpClientオブジェクトを作成する
        Dim udp As New System.Net.Sockets.UdpClient()
        '送信するデータを作成する
        Dim sendMsg As String = "I'm sensor host"
        Dim sendBytes As Byte() = System.Text.Encoding.UTF8.GetBytes(sendMsg)
        'リモートホストを指定してデータを送信する
        udp.Send(sendBytes, sendBytes.Length, remoteHost, remotePort)
        udp.Close()
    End Sub
    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        NotifyIcon1.BalloonTipText = "aaa"
        NotifyIcon1.BalloonTipIcon = ToolTipIcon.Error
        NotifyIcon1.ShowBalloonTip(3000)
        getCurrentBroadcastAddr()
    End Sub

    Private Function getCurrentBroadcastAddr()
        Dim nis As NetworkInterface() = NetworkInterface.GetAllNetworkInterfaces()
        Dim ni As NetworkInterface
        Dim ret As String = ""
        For Each ni In nis
            'ネットワーク接続しているか調べる
            If ni.OperationalStatus = OperationalStatus.Up AndAlso
                ni.NetworkInterfaceType <> NetworkInterfaceType.Loopback AndAlso
                ni.NetworkInterfaceType <> NetworkInterfaceType.Tunnel Then

                '構成情報、アドレス情報を取得する
                Dim ipips As IPInterfaceProperties = ni.GetIPProperties()
                If Not (ipips Is Nothing) Then
                    For Each ip As UnicastIPAddressInformation In ipips.UnicastAddresses
                        If ip.IPv4Mask.ToString = "0.0.0.0" Then Continue For

                        Console.WriteLine("ユニキャストアドレス:{0}", ip.Address)
                        Console.WriteLine("IPv4マスク:{0}", ip.IPv4Mask)
                        ret = CalcBroadcastAddr(ip.Address.ToString, ip.IPv4Mask.ToString)

                    Next ip
                End If
            End If
        Next ni
        Return ret
    End Function

    Private Function CalcBroadcastAddr(address As String, netmask As String)
        Dim addr_a() As String = Split(address, ".")
        Dim mask_a() As String = Split(netmask, ".")
        Dim ret As String = ""
        For i = 0 To 3
            Dim a As Integer
            Dim bitm As Integer = Integer.Parse(mask_a(i))
            If bitm = 255 Then
                a = Integer.Parse(addr_a(i)) And bitm
            Else
                Dim inv_bitm As Integer = 255 - bitm
                'Console.WriteLine("inv={0}", inv_bitm)
                a = Integer.Parse(addr_a(i)) Or inv_bitm
            End If
            ret = ret + a.ToString()
            If i < 3 Then ret = ret + "."
        Next i
        Console.WriteLine("Broadcast Address={0}", ret)
        Return ret
    End Function


End Class
