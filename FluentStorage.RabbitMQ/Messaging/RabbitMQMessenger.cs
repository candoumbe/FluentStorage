using FluentStorage.Messaging;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FluentStorage.RabbitMQ.Messaging {
	class RabbitMQMessenger : IRabbitMQMessenger {

		private readonly IConnection _connection;
		private readonly ConcurrentDictionary<string, (AsyncEventingBasicConsumer Consumer, AsyncEventHandler<BasicDeliverEventArgs> Handler, ISet<IMessageProcessor> Processors)> _declaredConsumers;


		/// <summary>
		/// Builds a new <see cref="RabbitMQMessenger"/> instance
		/// </summary>
		/// <param name="hostname">hotstname of the rabbit mq</param>
		/// <param name="port">Port used to connect to the RabbitMQ server</param>
		/// <param name="username"></param>
		/// <param name="password"></param>
		public RabbitMQMessenger(string hostname, int port, string username, string password) {
			ConnectionFactory connectionFactory = new() {
				HostName = hostname,
				Port = port,
				UserName = username,
				Password = password,
			};
			_connection = connectionFactory.CreateConnection();
			_declaredConsumers = new();

		}

		///<inheritdoc/>
		public Task CreateChannelsAsync(IEnumerable<string> channelNames, CancellationToken cancellationToken = default) {

			cancellationToken.ThrowIfCancellationRequested();

			using IModel channel = _connection.CreateModel();
			foreach (string channelName in channelNames) {

				channel.QueueDeclare(channelName, durable: true);
			}

			return Task.CompletedTask;
		}

		///<inheritdoc/>
		public Task DeleteAsync(string channelName, IEnumerable<QueueMessage> messages, CancellationToken cancellationToken = default) {
			throw new NotSupportedException("Rabbit MQ does not support deleting message from a queue");
		}

		///<inheritdoc/>
		public Task DeleteChannelsAsync(IEnumerable<string> channelNames, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();

			using IModel channel = _connection.CreateModel();
			foreach (string channelName in channelNames) {
				channel.QueueDelete(channelName, false, false);
			}
			channel.Close();
			return Task.CompletedTask;
		}

		///<inheritdoc/>
		public void Dispose() {
			_connection?.Close();
			_connection?.Dispose();
		}

		///<inheritdoc/>
		public Task<long> GetMessageCountAsync(string channelName, CancellationToken cancellationToken = default) {
			using IModel channel = _connection.CreateModel();

			long count = channel.MessageCount(channelName);

			channel.Close();

			return Task.FromResult(count);
		}

		///<inheritdoc/>
		public Task<IReadOnlyCollection<string>> ListChannelsAsync(CancellationToken cancellationToken = default) {

			throw new NotSupportedException("The current implementation does not allow to list channels");
		}

		///<inheritdoc/>
		public Task<IReadOnlyCollection<QueueMessage>> PeekAsync(string channelName, int count = 100, CancellationToken cancellationToken = default) {
			throw new NotSupportedException("Peeking messages is not supported");
		}

		///<inheritdoc/>
		public Task<IReadOnlyCollection<QueueMessage>> ReceiveAsync(string channelName, int count = 100, TimeSpan? visibility = null, CancellationToken cancellationToken = default) {
			throw new NotImplementedException();
		}

		///<inheritdoc/>
		public Task SendAsync(string channelName, IEnumerable<QueueMessage> messages, CancellationToken cancellationToken = default) {
			throw new NotImplementedException();
		}

		///<inheritdoc/>
		public Task StartMessageProcessorAsync(string channelName, IMessageProcessor messageProcessor) {

			HashSet<IMessageProcessor> processors;
			AsyncEventingBasicConsumer consumer;
			if (_declaredConsumers.TryRemove(channelName, out (AsyncEventingBasicConsumer Consumer, AsyncEventHandler<BasicDeliverEventArgs> Handler, ISet<IMessageProcessor> Processors) consumerAndProcessor)) {
				consumerAndProcessor.Consumer.Received -= consumerAndProcessor.Handler; // removes the old handler to avoid any memory leak.
				processors = new(consumerAndProcessor.Processors) { messageProcessor };
				consumer = consumerAndProcessor.Consumer;
			}
			else {
				using IModel channel = _connection.CreateModel();

				consumer = new AsyncEventingBasicConsumer(channel);
				processors = new HashSet<IMessageProcessor>() { messageProcessor };
			}
			AsyncEventHandler<BasicDeliverEventArgs> handler = async (sender, args) => {
				QueueMessage message = QueueMessage.FromByteArray(args.Body.ToArray());

				foreach (IMessageProcessor processor in consumerAndProcessor.Processors) {
					await processor.ProcessMessagesAsync(new[] { message }).ConfigureAwait(false);
				}

			};
			consumer.Received += handler;

			_declaredConsumers.TryAdd(channelName, (consumer, handler, processors));


			return Task.CompletedTask;
		}
	}
}