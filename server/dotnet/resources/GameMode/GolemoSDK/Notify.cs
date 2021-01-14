using GTANetworkAPI;
using System;
using System.Collections.Generic;
using System.Text;

namespace GolemoSDK
{
    public enum NotifyType
    {
        Alert,
        Error,
        Success,
        Info,
        Warning
    }
    public enum NotifyPosition
    {
        Top,
        TopLeft,
        TopCenter,
        TopRight,
        Center,
        CenterLeft,
        CenterRight,
        Bottom,
        BottomLeft,
        BottomCenter,
        BottomRight
    }
    public static class Notify
    {
        public static void Send(Player client, NotifyType type, NotifyPosition pos, string msg, int time)
        {
            Trigger.ClientEvent(client, "notify", type, pos, msg, time);
        }
        public static void Error(Player client, string msg, int time)
        {
            Trigger.ClientEvent(client, "notify", NotifyType.Error, NotifyPosition.BottomCenter, msg, time);
        }
        public static void Alert(Player client, string msg, int time)
        {
            Trigger.ClientEvent(client, "notify", NotifyType.Alert, NotifyPosition.BottomCenter, msg, time);
        }
        public static void Info(Player client, string msg, int time)
        {
            Trigger.ClientEvent(client, "notify", NotifyType.Info, NotifyPosition.BottomCenter, msg, time);
        }
        public static void Succ(Player client, string msg, int time)
        {
            Trigger.ClientEvent(client, "notify", NotifyType.Success, NotifyPosition.BottomCenter, msg, time);
        }
        public static void Warn(Player client, string msg, int time)
        {
            Trigger.ClientEvent(client, "notify", NotifyType.Warning, NotifyPosition.BottomCenter, msg, time);
        }
    }
}
