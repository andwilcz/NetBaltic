using System;
using ServiceStack;
using System.Runtime.Serialization;

namespace NetBaltic.ServerProxy {

	//Definicja obiektu, który do mnie wysyłasz
	public class InputMessageTransportModule {

	//Adres adbiorcy tekstowo jako IPv6 np "fe08:2dfa::1"
		public string Receiver { get; set; }

	//Czas ważności wiadomości, w postaci daty w przyszłości
		public DateTime ValidityTime { get; set; }

	//Priorytet wiadomości, zdefiniowane w Priorities.cs, najlepiej ustawić zawsze jako wartość NORMAL
		public Priorities Priority { get; set; }

	//Typ usługi, zdefiniowane w ServiceType.cs, to jest typ usługi do której wysyłasz wiadomość czyli w naszym przypadku będzie to WWW, wartość SERVER_PROXY zignoruj, nie używaj jej
		public ServiceType ServiceType { get; set; }

	//Zawartość wiadomości w postaci tekstu, jako string
		public string Content { get; set; }

		public InputMessageTransportModule ()
		{

		}
	}

}

