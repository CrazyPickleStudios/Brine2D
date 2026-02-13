using Brine2D.ECS;

namespace Brine2D.Tests.ECS;

public class DeferredListTests
{
    #region Basic Operations

    [Fact]
    public void Count_InitiallyEmpty_ReturnsZero()
    {
        // Arrange
        var list = new DeferredList<string>();

        // Act & Assert
        Assert.Equal(0, list.Count);
    }

    [Fact]
    public void Add_BeforeProcessing_DoesNotIncreaseCount()
    {
        // Arrange
        var list = new DeferredList<string>();

        // Act
        list.Add("Item1");

        // Assert - Count should still be 0 (add is deferred)
        Assert.Equal(0, list.Count);
    }

    [Fact]
    public void Add_AfterProcessing_IncreasesCount()
    {
        // Arrange
        var list = new DeferredList<string>();

        // Act
        list.Add("Item1");
        list.ProcessChanges();

        // Assert
        Assert.Equal(1, list.Count);
    }

    [Fact]
    public void Add_MultipleItems_AllAdded()
    {
        // Arrange
        var list = new DeferredList<string>();

        // Act
        list.Add("Item1");
        list.Add("Item2");
        list.Add("Item3");
        list.ProcessChanges();

        // Assert
        Assert.Equal(3, list.Count);
    }

    [Fact]
    public void Remove_ExistingItem_QueuesForRemoval()
    {
        // Arrange
        var list = new DeferredList<string>();
        list.Add("Item1");
        list.ProcessChanges();

        // Act
        list.Remove("Item1");

        // Assert - Still in list before processing
        Assert.Equal(1, list.Count);
        Assert.True(list.Contains("Item1"));
    }

    [Fact]
    public void Remove_AfterProcessing_RemovesItem()
    {
        // Arrange
        var list = new DeferredList<string>();
        list.Add("Item1");
        list.ProcessChanges();

        // Act
        list.Remove("Item1");
        list.ProcessChanges();

        // Assert
        Assert.Equal(0, list.Count);
        Assert.False(list.Contains("Item1"));
    }

    #endregion

    #region Contains Method

    [Fact]
    public void Contains_ItemExists_ReturnsTrue()
    {
        // Arrange
        var list = new DeferredList<string>();
        list.Add("Item1");
        list.ProcessChanges();

        // Act & Assert
        Assert.True(list.Contains("Item1"));
    }

    [Fact]
    public void Contains_ItemDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var list = new DeferredList<string>();

        // Act & Assert
        Assert.False(list.Contains("Item1"));
    }

    [Fact]
    public void Contains_PendingAdd_ReturnsFalse()
    {
        // Arrange
        var list = new DeferredList<string>();
        list.Add("Item1"); // Not processed yet

        // Act & Assert - Should not see pending adds
        Assert.False(list.Contains("Item1"));
    }

    [Fact]
    public void Contains_PendingRemoval_ReturnsTrue()
    {
        // Arrange
        var list = new DeferredList<string>();
        list.Add("Item1");
        list.ProcessChanges();
        list.Remove("Item1"); // Queued for removal

        // Act & Assert - Still contains until processed
        Assert.True(list.Contains("Item1"));
    }

    #endregion

    #region IsQueuedForRemoval Method

    [Fact]
    public void IsQueuedForRemoval_ItemQueued_ReturnsTrue()
    {
        // Arrange
        var list = new DeferredList<string>();
        list.Add("Item1");
        list.ProcessChanges();

        // Act
        list.Remove("Item1");

        // Assert
        Assert.True(list.IsQueuedForRemoval("Item1"));
    }

    [Fact]
    public void IsQueuedForRemoval_ItemNotQueued_ReturnsFalse()
    {
        // Arrange
        var list = new DeferredList<string>();
        list.Add("Item1");
        list.ProcessChanges();

        // Act & Assert
        Assert.False(list.IsQueuedForRemoval("Item1"));
    }

    [Fact]
    public void IsQueuedForRemoval_AfterProcessing_ReturnsFalse()
    {
        // Arrange
        var list = new DeferredList<string>();
        list.Add("Item1");
        list.ProcessChanges();
        list.Remove("Item1");

        // Act
        list.ProcessChanges();

        // Assert - No longer queued after processing
        Assert.False(list.IsQueuedForRemoval("Item1"));
    }

    #endregion

    #region HasPendingChanges Property

    [Fact]
    public void HasPendingChanges_NoPending_ReturnsFalse()
    {
        // Arrange
        var list = new DeferredList<string>();

        // Act & Assert
        Assert.False(list.HasPendingChanges);
    }

    [Fact]
    public void HasPendingChanges_PendingAdd_ReturnsTrue()
    {
        // Arrange
        var list = new DeferredList<string>();

        // Act
        list.Add("Item1");

        // Assert
        Assert.True(list.HasPendingChanges);
    }

    [Fact]
    public void HasPendingChanges_PendingRemoval_ReturnsTrue()
    {
        // Arrange
        var list = new DeferredList<string>();
        list.Add("Item1");
        list.ProcessChanges();

        // Act
        list.Remove("Item1");

        // Assert
        Assert.True(list.HasPendingChanges);
    }

    [Fact]
    public void HasPendingChanges_AfterProcessing_ReturnsFalse()
    {
        // Arrange
        var list = new DeferredList<string>();
        list.Add("Item1");

        // Act
        list.ProcessChanges();

        // Assert
        Assert.False(list.HasPendingChanges);
    }

    [Fact]
    public void HasPendingChanges_BothAddAndRemove_ReturnsTrue()
    {
        // Arrange
        var list = new DeferredList<string>();
        list.Add("Item1");
        list.ProcessChanges();

        // Act
        list.Add("Item2");
        list.Remove("Item1");

        // Assert
        Assert.True(list.HasPendingChanges);
    }

    #endregion

    #region ProcessChanges Method

    [Fact]
    public void ProcessChanges_AppliesAddsBeforeRemoves()
    {
        // Arrange
        var list = new DeferredList<string>();
        list.Add("Item1");
        list.ProcessChanges();

        // Act - Queue add and remove in same batch
        list.Add("Item2");
        list.Remove("Item1");
        list.ProcessChanges();

        // Assert - Item2 should be added, Item1 removed
        Assert.Equal(1, list.Count);
        Assert.True(list.Contains("Item2"));
        Assert.False(list.Contains("Item1"));
    }

    [Fact]
    public void ProcessChanges_MultipleTimes_Works()
    {
        // Arrange
        var list = new DeferredList<string>();

        // Act - Process multiple times
        list.Add("Item1");
        list.ProcessChanges();

        list.Add("Item2");
        list.ProcessChanges();

        list.Add("Item3");
        list.ProcessChanges();

        // Assert
        Assert.Equal(3, list.Count);
    }

    [Fact]
    public void ProcessChanges_EmptyQueues_DoesNothing()
    {
        // Arrange
        var list = new DeferredList<string>();
        list.Add("Item1");
        list.ProcessChanges();

        // Act - Process with no pending changes
        list.ProcessChanges();

        // Assert
        Assert.Equal(1, list.Count);
    }

    #endregion

    #region ProcessAdds Method

    [Fact]
    public void ProcessAdds_CallsCallbackForEachItem()
    {
        // Arrange
        var list = new DeferredList<string>();
        var addedItems = new List<string>();

        list.Add("Item1");
        list.Add("Item2");
        list.Add("Item3");

        // Act
        list.ProcessAdds(item => addedItems.Add(item));

        // Assert
        Assert.Equal(3, addedItems.Count);
        Assert.Contains("Item1", addedItems);
        Assert.Contains("Item2", addedItems);
        Assert.Contains("Item3", addedItems);
        Assert.Equal(3, list.Count);
    }

    [Fact]
    public void ProcessAdds_NoItemsQueued_DoesNotCallCallback()
    {
        // Arrange
        var list = new DeferredList<string>();
        var callbackCalled = false;

        // Act
        list.ProcessAdds(item => callbackCalled = true);

        // Assert
        Assert.False(callbackCalled);
    }

    [Fact]
    public void ProcessAdds_ClearsPendingAdds()
    {
        // Arrange
        var list = new DeferredList<string>();
        list.Add("Item1");

        // Act
        list.ProcessAdds(item => { });

        // Assert
        Assert.False(list.HasPendingChanges);
    }

    #endregion

    #region ProcessRemovals Method

    [Fact]
    public void ProcessRemovals_CallsCallbackForEachItem()
    {
        // Arrange
        var list = new DeferredList<string>();
        list.Add("Item1");
        list.Add("Item2");
        list.Add("Item3");
        list.ProcessChanges();

        var removedItems = new List<string>();
        list.Remove("Item1");
        list.Remove("Item2");

        // Act
        list.ProcessRemovals(item => removedItems.Add(item));

        // Assert
        Assert.Equal(2, removedItems.Count);
        Assert.Contains("Item1", removedItems);
        Assert.Contains("Item2", removedItems);
        Assert.Equal(1, list.Count);
        Assert.True(list.Contains("Item3"));
    }

    [Fact]
    public void ProcessRemovals_NoItemsQueued_DoesNotCallCallback()
    {
        // Arrange
        var list = new DeferredList<string>();
        var callbackCalled = false;

        // Act
        list.ProcessRemovals(item => callbackCalled = true);

        // Assert
        Assert.False(callbackCalled);
    }

    [Fact]
    public void ProcessRemovals_ItemNotInList_DoesNotCallCallback()
    {
        // Arrange
        var list = new DeferredList<string>();
        list.Add("Item1");
        list.ProcessChanges();
        
        list.Remove("NonExistent");
        var removedItems = new List<string>();

        // Act
        list.ProcessRemovals(item => removedItems.Add(item));

        // Assert - Callback should not be called for non-existent items
        Assert.Empty(removedItems);
    }

    [Fact]
    public void ProcessRemovals_ClearsPendingRemovals()
    {
        // Arrange
        var list = new DeferredList<string>();
        list.Add("Item1");
        list.ProcessChanges();
        list.Remove("Item1");

        // Act
        list.ProcessRemovals(item => { });

        // Assert
        Assert.False(list.HasPendingChanges);
    }

    #endregion

    #region Clear Method

    [Fact]
    public void Clear_RemovesAllItems()
    {
        // Arrange
        var list = new DeferredList<string>();
        list.Add("Item1");
        list.Add("Item2");
        list.ProcessChanges();

        // Act
        list.Clear();

        // Assert
        Assert.Equal(0, list.Count);
    }

    [Fact]
    public void Clear_ClearsPendingChanges()
    {
        // Arrange
        var list = new DeferredList<string>();
        list.Add("Item1");
        list.Add("Item2");

        // Act
        list.Clear();

        // Assert
        Assert.False(list.HasPendingChanges);
        Assert.Equal(0, list.Count);
    }

    [Fact]
    public void Clear_ClearsPendingAddsAndRemovals()
    {
        // Arrange
        var list = new DeferredList<string>();
        list.Add("Item1");
        list.ProcessChanges();
        list.Add("Item2");
        list.Remove("Item1");

        // Act
        list.Clear();

        // Assert
        Assert.Equal(0, list.Count);
        Assert.False(list.HasPendingChanges);
    }

    #endregion

    #region AsReadOnly Method

    [Fact]
    public void AsReadOnly_ReturnsReadOnlyView()
    {
        // Arrange
        var list = new DeferredList<string>();
        list.Add("Item1");
        list.Add("Item2");
        list.ProcessChanges();

        // Act
        var readOnly = list.AsReadOnly();

        // Assert
        Assert.Equal(2, readOnly.Count);
        Assert.Contains("Item1", readOnly);
        Assert.Contains("Item2", readOnly);
    }

    [Fact]
    public void AsReadOnly_DoesNotIncludePendingChanges()
    {
        // Arrange
        var list = new DeferredList<string>();
        list.Add("Item1");
        list.ProcessChanges();
        list.Add("Item2"); // Pending add

        // Act
        var readOnly = list.AsReadOnly();

        // Assert
        Assert.Single(readOnly);
        Assert.Contains("Item1", readOnly);
    }

    [Fact]
    public void AsReadOnly_ReflectsProcessedChanges()
    {
        // Arrange
        var list = new DeferredList<string>();
        list.Add("Item1");
        list.ProcessChanges();

        var readOnly1 = list.AsReadOnly();
        Assert.Single(readOnly1);

        // Act - Add more and process
        list.Add("Item2");
        list.ProcessChanges();

        var readOnly2 = list.AsReadOnly();

        // Assert
        Assert.Equal(2, readOnly2.Count);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void DeferredList_ComplexScenario_WorksCorrectly()
    {
        // Arrange
        var list = new DeferredList<int>();

        // Act - Add initial items
        list.Add(1);
        list.Add(2);
        list.Add(3);
        list.ProcessChanges();
        Assert.Equal(3, list.Count);

        // Queue mixed operations
        list.Add(4);
        list.Add(5);
        list.Remove(2);
        list.ProcessChanges();

        // Assert
        Assert.Equal(4, list.Count);
        Assert.True(list.Contains(1));
        Assert.False(list.Contains(2)); // Removed
        Assert.True(list.Contains(3));
        Assert.True(list.Contains(4));
        Assert.True(list.Contains(5));
    }

    [Fact]
    public void DeferredList_DoubleRemove_HandlesSafely()
    {
        // Arrange
        var list = new DeferredList<string>();
        list.Add("Item1");
        list.ProcessChanges();

        // Act - Try to remove same item twice
        list.Remove("Item1");
        list.Remove("Item1");
        list.ProcessChanges();

        // Assert - Should handle gracefully
        Assert.Equal(0, list.Count);
    }

    [Fact]
    public void DeferredList_RemoveBeforeAdd_HandlesSafely()
    {
        // Arrange
        var list = new DeferredList<string>();

        // Act - Try to remove item that was never added
        list.Remove("NonExistent");
        list.ProcessChanges();

        // Assert - Should not throw
        Assert.Equal(0, list.Count);
    }

    [Fact]
    public void DeferredList_SafeIteration_WorksCorrectly()
    {
        // Arrange
        var list = new DeferredList<int>();
        list.Add(1);
        list.Add(2);
        list.Add(3);
        list.ProcessChanges();

        // Act - Iterate and queue changes during iteration
        var sum = 0;
        foreach (var item in list)
        {
            sum += item;
            if (item == 2)
            {
                list.Remove(item); // Queue removal during iteration
                list.Add(4);        // Queue add during iteration
            }
        }

        // Assert - Iteration completed without error
        Assert.Equal(6, sum); // 1 + 2 + 3

        // Process and verify queued changes
        list.ProcessChanges();
        Assert.Equal(3, list.Count); // 1, 3, 4
        Assert.True(list.Contains(1));
        Assert.False(list.Contains(2));
        Assert.True(list.Contains(3));
        Assert.True(list.Contains(4));
    }

    #endregion
}