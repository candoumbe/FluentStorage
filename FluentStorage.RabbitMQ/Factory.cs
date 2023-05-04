using FluentStorage.ConnectionString;
using FluentStorage.Messaging;
using FluentStorage.RabbitMQ;
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
		public static IRabbitMqMessenger RabbitMq(this IMessagingFactory factory, StorageConnectionString connectionString) {
			IRabbitMqMessenger messenger = null;

			if (connectionString.Prefix == "rabbitmq") {

				string hostname, username, password;

				connectionString.GetRequired(nameof(hostname), true, out hostname);
				connectionString.GetRequired(nameof(username), true, out username);
				connectionString.GetRequired(nameof(password), true, out password);
				int port = int.TryParse(connectionString.Get(nameof(port)), out port) ? port : 25;

				messenger = new RabbitMqMessenger(hostname, port, username, password);
			}

			return messenger;
		}
	}
}
