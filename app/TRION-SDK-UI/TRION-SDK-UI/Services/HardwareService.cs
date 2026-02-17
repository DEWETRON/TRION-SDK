using System.Diagnostics;
using Trion;
using TRION_SDK_UI.Models;
using TrionApiUtils;

namespace TRION_SDK_UI.Services;

public sealed class HardwareService : IDisposable
{
    private bool _initialized;
    private static readonly string _ipAddress = "10.0.0.100";
    private static readonly string _mask = "255.255.0.0";


    public Enclosure Enclosure { get; } = new Enclosure { Name = string.Empty, Boards = [] };

    public HardwareInitResult Initialize()
    {
        API.DeWeConfigure(API.Backend.TRIONET);

        var error = TrionApi.DeWeSetParamStruct("trionetapi/config", "Network/IPV4/LocalIP", _ipAddress);
        Utils.CheckErrorCode(error, "Failed to set local IP address");

        error = TrionApi.DeWeSetParamStruct("trionetapi/config", "Network/IPV4/NetMask", _mask);
        Utils.CheckErrorCode(error, "Failed to set subnet mask");

        var numberOfBoards = TrionApi.Initialize();
        _initialized = true;

        if (numberOfBoards == 0)
        {
            return new HardwareInitResult(0, false);
        }

        var isSimulated = numberOfBoards < 0;
        var boardCount = Math.Abs(numberOfBoards);

        Enclosure.Init(boardCount);

        return new HardwareInitResult(boardCount, isSimulated);
    }

    public IReadOnlyList<Channel> GetChannels(params Channel.ChannelType[] types)
    {
        var result = new List<Channel>();
        foreach (var board in Enclosure.Boards)
        {
            result.AddRange(board.Channels.Where(c => types.Contains(c.Type)));
        }
        return result;
    }

    public void Dispose()
    {
        if (!_initialized)
        {
            return;
        }

        TrionApi.DeWeSetParam_i32(0, TrionCommand.CLOSE_BOARD_ALL, 0);
        TrionApi.Uninitialize();
        _initialized = false;

        Debug.WriteLine("HardwareService disposed — driver shut down.");
    }
}

public readonly record struct HardwareInitResult(int BoardCount, bool IsSimulated);