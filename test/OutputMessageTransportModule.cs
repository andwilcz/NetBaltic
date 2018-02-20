using System;
using System.Net;
using ServiceStack;

namespace NetBaltic.ServerProxy {

	//Definicja obiektu, który otrzymujesz od modułu transportowego
	public class OutputMessageTransportModule {
		//Kto wysłał w postaci adresu IPv6, jako string
		[ApiMember(IsRequired = true, Description = "The sender's address (IPv6)")]
		public string Sender { get; set; }
		//Zawartość jako string
		[ApiMember(IsRequired = true, Description = "Data to receive")]
		public string Content { get; set; }
		//Jaka usługa wysłała wiadomość
		[ApiMember(IsRequired = true, Description = "Service Type")]
		[ApiAllowableValues("SERVER_PROXY", "MAIL", "WWW")]
		public ServiceType ServiceType { get; set; }
	}


}

