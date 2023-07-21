using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// 클라에서 먼저 요청 -> Req
public class CreateAccountPacketReq
{
    public string AccountName { get; set; }
    public string Password { get; set; }
}

// 서버의 응답 -> Res
public class CreateAccountPacketRes
{
    public bool CreateOk { get; set; }
}

public class LoginAccountPacketReq
{
    public string AccountName { get; set; }
    public string Password { get; set; }
}

public class ServerInfo
{
    public string Name { get; set; }
    public string IpAddress { get; set; }
    public int Port { get; set; }
    public int ByshScore { get; set; }
}


public class LoginAccountPacketRes
{
    public bool LoginOk { get; set; }
    public int AccountId { get; set; }
    public int Token { get; set; }
}