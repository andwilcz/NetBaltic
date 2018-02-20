using System;
using ServiceStack;
using System.Net;

namespace NetBaltic.ServerProxy {
	
	public enum WebServiceDataFormat {
		JSON = 0x01,
		JSV = 0x02,
		CSV = 0x03
	}

	public static class WebServiceDataFormatExtensions {

		public static IServiceClient GetServiceClient(WebServiceDataFormat wbdf, string ipAddress) {
			switch (wbdf) {
				case WebServiceDataFormat.JSON: return new JsonServiceClient(ipAddress);
				case WebServiceDataFormat.JSV: return new JsvServiceClient(ipAddress);
				default: return new CsvServiceClient(ipAddress);
			}
		}
	}
}
