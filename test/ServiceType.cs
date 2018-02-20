using System;
using System.Collections.Generic;

namespace NetBaltic.ServerProxy {
	
	public enum ServiceType : byte {
		SERVER_PROXY = 0x01,
		MAIL = 0x02,
		WWW = 0x03
	}

	public static class ServiceTypeExtensions {

		public static ServiceType GetServiceType (byte st) {
			switch (st) {
				case 0x01: return ServiceType.SERVER_PROXY;
				case 0x02: return ServiceType.MAIL;
				default: return ServiceType.WWW;
			}
		}
	}
}

