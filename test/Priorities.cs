using System;
using System.Runtime.Serialization;

namespace NetBaltic.ServerProxy {

	public enum Priorities : byte {
		EXPEDITED = 0x00,
		NORMAL = 0x01,
		BULK = 0x02
	}
}

