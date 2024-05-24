// ReSharper disable once InconsistentNaming
// ReSharper disable InconsistentNaming
public sealed record RespRDB : RespValue
{
    private static readonly byte[] EmptyRDB = [
        0x52, 0x45, 0x44, 0x49, 0x53, 0x30, 0x30, 0x31, 0x31, 0xfa, 0x09, 0x72, 0x65, 0x64, 0x69, 0x73, //  |REDIS0011..redis|
        0x2d, 0x76, 0x65, 0x72, 0x05, 0x37, 0x2e, 0x32, 0x2e, 0x30, 0xfa, 0x0a, 0x72, 0x65, 0x64, 0x69, //  |-ver.7.2.0..redi|
        0x73, 0x2d, 0x62, 0x69, 0x74, 0x73, 0xc0, 0x40, 0xfa, 0x05, 0x63, 0x74, 0x69, 0x6d, 0x65, 0xc2, //  |s-bits.@..ctime.|
        0x6d, 0x08, 0xbc, 0x65, 0xfa, 0x08, 0x75, 0x73, 0x65, 0x64, 0x2d, 0x6d, 0x65, 0x6d, 0xc2, 0xb0, //  |m..e..used-mem..|
        0xc4, 0x10, 0x00, 0xfa, 0x08, 0x61, 0x6f, 0x66, 0x2d, 0x62, 0x61, 0x73, 0x65, 0xc0, 0x00, 0xff, //  |.....aof-base...|
        0xf0, 0x6e, 0x3b, 0xfe, 0xc0, 0xff, 0x5a, 0xa2                                                  //  |.n;...Z.|
    ];

    public override ArraySegment<byte> Encode()
    {
        var value = new RespString($"FULLRESYNC {ServerInfo.GetReplId()} {ServerInfo.GetMasterReplOffset()}");
        var encoded = value.Encode();
        return new ArraySegment<byte>([..encoded, .."$"u8, ..EmptyRDB]);
    }
}