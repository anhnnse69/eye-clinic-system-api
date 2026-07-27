using ECS.Application.Services.PatientAppointmentManagementServices.ViewNotificationListServices;
using ECS.Domain.Entities.Notifications;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace ECS.Test.Services.PatientAppointmentManagementServices.ViewNotificationListServices
{
    public class ViewNotificationListServicesTests
    {
        private readonly Mock<IRepositoryQueryBase<Notification, Guid, AppDbContext>> _mockNotificationQueryRepo = new();
        private readonly Mock<IRepositoryBaseAsync<Notification, Guid, AppDbContext>> _mockNotificationCommandRepo = new();

        private static AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var context = new AppDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        private void SetupNotificationQueryRepo(IEnumerable<Notification> notifications)
        {
            _mockNotificationQueryRepo
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<Notification, bool>>>(), It.IsAny<bool>()))
                .Returns((Expression<Func<Notification, bool>> expression, bool trackChanges) =>
                {
                    var filteredList = notifications.AsQueryable().Where(expression).ToList();
                    return filteredList.BuildMockDbSet().Object;
                });
        }

        #region MarkNotificationReadService Tests

        [Fact]
        public async Task MarkNotificationRead_ShouldSucceed_WhenNotificationBelongsToUser()
        {
            // Arrange
            var userId = ViewNotificationListServiceMockData.ValidUserId;
            var notificationId = ViewNotificationListServiceMockData.NotificationId1;
            var notification = ViewNotificationListServiceMockData.CreateNotification(id: notificationId, userId: userId, isRead: false);

            SetupNotificationQueryRepo(new[] { notification });

            _mockNotificationCommandRepo
                .Setup(r => r.UpdateAsync(It.IsAny<Notification>()))
                .Returns(Task.CompletedTask);
            _mockNotificationCommandRepo
                .Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);

            var service = new MarkNotificationReadService(
                _mockNotificationQueryRepo.Object,
                _mockNotificationCommandRepo.Object);

            // Act
            var result = await service.Process(userId, notificationId);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().Be("OK");

            notification.IsRead.Should().BeTrue();
            _mockNotificationCommandRepo.Verify(r => r.UpdateAsync(It.Is<Notification>(n => n.Id == notificationId && n.IsRead)), Times.Once);
            _mockNotificationCommandRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task MarkNotificationRead_ShouldThrowKeyNotFoundException_WhenNotificationNotFoundOrNotOwned()
        {
            // Arrange
            var userId = ViewNotificationListServiceMockData.ValidUserId;
            var notificationId = ViewNotificationListServiceMockData.NotificationId1;

            // List empty -> query returns null
            SetupNotificationQueryRepo(Enumerable.Empty<Notification>());

            var service = new MarkNotificationReadService(
                _mockNotificationQueryRepo.Object,
                _mockNotificationCommandRepo.Object);

            // Act
            Func<Task> act = async () => await service.Process(userId, notificationId);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4004.ToString());

            _mockNotificationCommandRepo.Verify(r => r.UpdateAsync(It.IsAny<Notification>()), Times.Never);
            _mockNotificationCommandRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        #endregion

        #region NotificationCreationService Tests

        [Fact]
        public async Task NotificationCreation_ShouldCreateAndSaveNotification_Successfully()
        {
            // Arrange
            var userId = ViewNotificationListServiceMockData.ValidUserId;
            const string title = "Appointment Confirmed";
            const string content = "Your appointment has been confirmed.";

            Notification? capturedNotification = null;

            _mockNotificationCommandRepo
                .Setup(r => r.CreateAsync(It.IsAny<Notification>()))
                .Callback<Notification>(n => capturedNotification = n)
                .ReturnsAsync(Guid.NewGuid());

            _mockNotificationCommandRepo
                .Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);

            var service = new NotificationCreationService(_mockNotificationCommandRepo.Object);

            // Act
            var result = await service.Process(userId, title, content);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().Be("OK");

            capturedNotification.Should().NotBeNull();
            capturedNotification!.UserId.Should().Be(userId);
            capturedNotification.Title.Should().Be(title);
            capturedNotification.Content.Should().Be(content);
            capturedNotification.IsRead.Should().BeFalse();
            capturedNotification.SentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

            _mockNotificationCommandRepo.Verify(r => r.CreateAsync(It.IsAny<Notification>()), Times.Once);
            _mockNotificationCommandRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region ViewNotificationListService Tests
        [Fact]
        public void CalculateTotalPages_ShouldReturnZero_WhenPageSizeIsZero()
        {
            // Arrange: Sử dụng Reflection để gọi phương thức private static CalculateTotalPages
            var methodInfo = typeof(ViewNotificationListService)
                .GetMethod("CalculateTotalPages", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            methodInfo.Should().NotBeNull("Phương thức CalculateTotalPages phải tồn tại");

            // Act: Truyền totalRecords = 10 và pageSize = 0 để đi vào nhánh `pageSize == 0 ? 0`
            var result = (int)methodInfo!.Invoke(null, new object[] { 10, 0 })!;

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public async Task ViewNotificationList_ShouldReturnAllNotifications_WhenNoIsReadFilterSpecified()
        {
            // Arrange
            var userId = ViewNotificationListServiceMockData.ValidUserId;
            var notifications = ViewNotificationListServiceMockData.GetSampleNotifications(userId);

            SetupNotificationQueryRepo(notifications);

            var service = new ViewNotificationListService(_mockNotificationQueryRepo.Object);
            var request = ViewNotificationListServiceMockData.CreateRequest(pageNumber: 1, pageSize: 10, isRead: null);

            // Act
            var result = await service.Process(userId, request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();

            var response = result.Data!;
            response.PageNumber.Should().Be(1);
            response.PageSize.Should().Be(10);
            response.TotalRecords.Should().Be(2);
            response.TotalPages.Should().Be(1);
            response.UnreadCount.Should().Be(1);
            response.Notifications.Should().HaveCount(2);

            // Verified OrderByDescending(SentAt)
            response.Notifications[0].Id.Should().Be(ViewNotificationListServiceMockData.NotificationId2);
            response.Notifications[1].Id.Should().Be(ViewNotificationListServiceMockData.NotificationId1);
        }

        [Fact]
        public async Task ViewNotificationList_ShouldFilterByIsRead_WhenFilterProvided()
        {
            // Arrange
            var userId = ViewNotificationListServiceMockData.ValidUserId;
            var notifications = ViewNotificationListServiceMockData.GetSampleNotifications(userId);

            SetupNotificationQueryRepo(notifications);

            var service = new ViewNotificationListService(_mockNotificationQueryRepo.Object);

            // Khai báo rõ ràng biến filter isRead
            bool? isReadFilter = false;
            var request = ViewNotificationListServiceMockData.CreateRequest(pageNumber: 1, pageSize: 10, isRead: isReadFilter);

            // Act
            var result = await service.Process(userId, request);

            // Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            var response = result.Data!;
            response.TotalRecords.Should().Be(1);
            response.UnreadCount.Should().Be(1);
            response.Notifications.Should().HaveCount(1);
            response.Notifications[0].Id.Should().Be(ViewNotificationListServiceMockData.NotificationId1);
            response.Notifications[0].IsRead.Should().BeFalse();
        }

        [Theory]
        [InlineData(0, 0, 1, 10)]   // Test NormalizePaging boundary (< 1)
        [InlineData(-5, -10, 1, 10)] // Test negative boundary
        [InlineData(2, 5, 2, 5)]     // Test valid custom paging
        public async Task ViewNotificationList_ShouldNormalizePagingParametersCorrectly(
            int inputPageNumber,
            int inputPageSize,
            int expectedPageNumber,
            int expectedPageSize)
        {
            // Arrange
            var userId = ViewNotificationListServiceMockData.ValidUserId;
            var notifications = ViewNotificationListServiceMockData.GetSampleNotifications(userId);

            SetupNotificationQueryRepo(notifications);

            var service = new ViewNotificationListService(_mockNotificationQueryRepo.Object);
            var request = ViewNotificationListServiceMockData.CreateRequest(pageNumber: inputPageNumber, pageSize: inputPageSize);

            // Act
            var result = await service.Process(userId, request);

            // Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            var response = result.Data!;
            response.PageNumber.Should().Be(expectedPageNumber);
            response.PageSize.Should().Be(expectedPageSize);
        }

        [Fact]
        public async Task ViewNotificationList_ShouldReturnZeroTotalPages_WhenTotalRecordsIsZero()
        {
            // Arrange
            var userId = ViewNotificationListServiceMockData.ValidUserId;

            SetupNotificationQueryRepo(Enumerable.Empty<Notification>());

            var service = new ViewNotificationListService(_mockNotificationQueryRepo.Object);
            var request = ViewNotificationListServiceMockData.CreateRequest(pageNumber: 1, pageSize: 10);

            // Act
            var result = await service.Process(userId, request);

            // Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            var response = result.Data!;
            response.TotalRecords.Should().Be(0);
            response.TotalPages.Should().Be(0);
            response.UnreadCount.Should().Be(0);
            response.Notifications.Should().BeEmpty();
        }

        #endregion
    }
}
