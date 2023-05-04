using FluentStorage.ConnectionString;
using FluentStorage.Messaging;
using FluentStorage.RabbitMQ.Messaging;

namespace FluentStorage {
	/// <summary>
	/// Factory class that implement factory methods for Microsoft Azure implememtations
	/// </summary>
	public static class Factory {

		/// <summary>
		/// Creates a new instance of Azure Service Bus Queue by connection string and queue name
		/// </summary>
		/// <param name="factory">Factory reference</param>
		/// <param name="connectionString">Service Bus connection string pointing to a namespace or an entity</param>
		public static IMessenger RabbitMQ(this IMessagingFactory factory,
		   StorageConnectionString connectionString) {

			connectionString.GetRequired("hostname", true, out string hostname);
			int.TryParse(connectionString.Get("port"), out int port);
			connectionString.GetRequired("username", true, out string username);
			connectionString.GetRequired("password", true, out string password);

			return new RabbitMQMessenger(hostname, port, username, password);
		}
	}
}
