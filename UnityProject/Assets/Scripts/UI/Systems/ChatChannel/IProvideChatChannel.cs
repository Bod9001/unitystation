using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IProvideChatChannel
{
	ChatChannel GetAvailableChannelsMask(bool transmitOnly = true);
}
