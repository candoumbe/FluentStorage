using FluentAssertions;

using FluentStorage.ConnectionString;
using FluentStorage.Messaging;

using Moq;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

using Xunit;
using Xunit.Extensions.AssemblyFixture;

namespace FluentStorage.RabbitMQ.Tests.Messaging {


	public class RabbitMQMessagingTest : IClassFixture<RabbitMqFixture>, IAsyncLifetime {
		private IMessenger _sut;
		private readonly RabbitMqFixture _rabbitMqFixture;

		public RabbitMQMessagingTest(RabbitMqFixture rabbitMqFixture) {
			_rabbitMqFixture = rabbitMqFixture;
		}

		///<inheritdoc/>
		public Task DisposeAsync() {

			_sut.Dispose();

			return Task.CompletedTask;
		}

		///<inheritdoc/>
		public async Task InitializeAsync() {
			await _rabbitMqFixture.InitializeAsync().ConfigureAwait(false);
			_sut = StorageFactory.Messages.RabbitMq(new StorageConnectionString($"rabbitmq://{_rabbitMqFixture.Container.GetConnectionString()}"));
		}

		[Fact]
		public void Should_throw_ArgumentNullException_when_channelName_is_null() {
			// Assert
			IMessageProcessor messageProcessor = Mock.Of<IMessageProcessor>();

			// Act
			Func<Task> startingMessageProcessorWhenChannelNameIsNull = async () => await _sut.StartMessageProcessorAsync(null, messageProcessor)
																							 .ConfigureAwait(false);

			// Assert
			startingMessageProcessorWhenChannelNameIsNull.Should()
														 .ThrowExactlyAsync<ArgumentNullException>("channelName cannot be null")
														 .Where(ex => !string.IsNullOrWhiteSpace(ex.ParamName));
		}

		[Fact]
		public void Should_throw_ArgumentNullException_when_messageProcessor_is_null() {
			// Assert
			string channelName = Guid.NewGuid().ToString();

			// Act
			Func<Task> startingMessageProcessorWhenChannelNameIsNull = async () => await _sut.StartMessageProcessorAsync(channelName, null)
																							 .ConfigureAwait(false);

			// Assert
			startingMessageProcessorWhenChannelNameIsNull.Should()
														 .ThrowExactlyAsync<ArgumentNullException>("message processor cannot be null")
														 .Where(ex => !string.IsNullOrWhiteSpace(ex.ParamName));
		}

		[Fact]
		public async Task Should_throw_when_channel_does_not_exist() {
			// Arrange
			string channelName = Guid.NewGuid().ToString();

			// Act
			Func<Task> startingMessageProcessorWhenChannelDoesNotExist = async () => await _sut.StartMessageProcessorAsync(channelName, Mock.Of<IMessageProcessor>())
																							  .ConfigureAwait(false);

			// Assert
			await startingMessageProcessorWhenChannelDoesNotExist.Should().ThrowAsync<Exception>("the specified channel does not exist");
		}

		[Fact]
		public async Task Given_one_processor_process_a_channel_When_an_event_is_sent_in_that_channel_Then_the_process_should_receive_the_event() {
			// Arrange
			string channelName = Guid.NewGuid().ToString();
			QueueMessage message = QueueMessage.FromText(Guid.NewGuid().ToString());

			await _sut.CreateChannelAsync(channelName).ConfigureAwait(false);

			Mock<IMessageProcessor> messageProcessorMock = new();

			// Act
			await _sut.StartMessageProcessorAsync(channelName, messageProcessorMock.Object).ConfigureAwait(false);
			await _sut.SendAsync(channelName, message).ConfigureAwait(false);

			// Assert
			messageProcessorMock.Verify(mock => mock.ProcessMessagesAsync(It.IsAny<IReadOnlyCollection<QueueMessage>>()), Times.Once);
			messageProcessorMock.Verify(mock => mock.ProcessMessagesAsync(It.Is<IReadOnlyCollection<QueueMessage>>(messages => messages.Count == 1
																															   && messages.ElementAt(0).StringContent == message.StringContent)),
																												   Times.Once);

		}

		[Fact]
		public async Task Given_many_processors_process_a_channel_When_an_event_is_sent_in_that_channel_Then_all_processors_should_receive_the_event() {
			// Arrange
			string channelName = Guid.NewGuid().ToString();
			QueueMessage message = QueueMessage.FromText(Guid.NewGuid().ToString());

			await _sut.CreateChannelAsync(channelName).ConfigureAwait(false);

			MockRepository mockRepository = new(MockBehavior.Loose);

			Mock<IMessageProcessor> firstProcessorMock = mockRepository.Create<IMessageProcessor>();
			Mock<IMessageProcessor> secondProcessorMock = mockRepository.Create<IMessageProcessor>();
			Mock<IMessageProcessor> thirdProcessorMock = mockRepository.Create<IMessageProcessor>();

			// Act
			await _sut.StartMessageProcessorAsync(channelName, firstProcessorMock.Object).ConfigureAwait(false);
			await _sut.StartMessageProcessorAsync(channelName, secondProcessorMock.Object).ConfigureAwait(false);
			await _sut.StartMessageProcessorAsync(channelName, thirdProcessorMock.Object).ConfigureAwait(false);

			await _sut.SendAsync(channelName, message).ConfigureAwait(false);

			// Assert
			firstProcessorMock.Verify(mock => mock.ProcessMessagesAsync(It.Is<IReadOnlyCollection<QueueMessage>>(messages => messages.Count == 1
																															 && messages.ElementAt(0).StringContent == message.StringContent)),
																		Times.Once);
			firstProcessorMock.VerifyNoOtherCalls();

			secondProcessorMock.Verify(mock => mock.ProcessMessagesAsync(It.Is<IReadOnlyCollection<QueueMessage>>(messages => messages.Count == 1
																		 													 && messages.ElementAt(0).StringContent == message.StringContent)),
																		Times.Once);
			secondProcessorMock.VerifyNoOtherCalls();

			thirdProcessorMock.Verify(mock => mock.ProcessMessagesAsync(It.Is<IReadOnlyCollection<QueueMessage>>(messages => messages.Count == 1
																															 && messages.ElementAt(0).StringContent == message.StringContent)),
																		Times.Once);
			thirdProcessorMock.VerifyNoOtherCalls();
		}
	}


	public class StoreEventMessageProcessor : IMessageProcessor {

		public IReadOnlyCollection<QueueMessage> Messages => _messages.ToImmutableArray();

		private readonly List<QueueMessage> _messages;

		public StoreEventMessageProcessor() {
			_messages = new List<QueueMessage>();
		}

		///<inheritdoc/>
		public Task ProcessMessagesAsync(IReadOnlyCollection<QueueMessage> messages) {
			_messages.AddRange(messages);

			return Task.CompletedTask;
		}
	}
}
