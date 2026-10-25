using SmartX.Api.Collections;

//ST10445500 - PROG7312 - SmartX POE
//AvlTreeTests

//.....................................o0oSTART OF FILEo0o........................................//

// The unit tests check the data structures without needing the API to be running.

namespace SmartX.Tests
{
    //checks that the tree stays balanced however the keys arrive, ranges include both ends, and a repeated key keeps every value
    public class AvlTreeTests
    {
        private static AvlTree<int, int> Build(IEnumerable<int> keys)
        {
            var tree = new AvlTree<int, int>();

            foreach (var key in keys)
            {
                tree.Insert(key, key);
            }

            return tree;
        }

        [Fact]
        public void GetRange_EmptyTree_IsEmptyWithNoHeight()
        {
            // Arrange
            var tree = new AvlTree<int, int>();

            // Act
            var found = tree.GetRange(0, 100);

            // Assert
            Assert.Empty(found);
            Assert.Equal(0, tree.Count);
            Assert.Equal(0, tree.Height);
        }

        [Fact]
        public void Insert_OneItem_HasHeightOneAndIsFoundOnlyInsideTheRange()
        {
            // Arrange
            var tree = Build([5]);

            // Act
            var hit = tree.GetRange(5, 5);
            var missBelow = tree.GetRange(1, 4);
            var missAbove = tree.GetRange(6, 9);

            // Assert
            Assert.Equal(1, tree.Height);
            Assert.Equal([5], hit);
            Assert.Empty(missBelow);
            Assert.Empty(missAbove);
        }

        [Fact]
        public void Insert_AscendingKeys_StaysPerfectlyBalanced()
        {
            // Arrange
            var tree = new AvlTree<int, int>();

            // Act
            for (var i = 1; i <= 1023; i++)
            {
                tree.Insert(i, i);
            }

            // Assert
            Assert.Equal(1023, tree.Count);
            Assert.Equal(10, tree.Height);
        }

        [Fact]
        public void Insert_DescendingKeys_StaysBalanced()
        {
            // Arrange
            var tree = new AvlTree<int, int>();

            // Act
            for (var i = 1023; i >= 1; i--)
            {
                tree.Insert(i, i);
            }

            // Assert
            Assert.Equal(10, tree.Height);
            Assert.Equal(Enumerable.Range(1, 1023), tree.GetRange(1, 1023));
        }

        [Theory]
        [InlineData(10, 30, 20)]
        [InlineData(30, 10, 20)]
        [InlineData(10, 20, 30)]
        [InlineData(30, 20, 10)]
        public void Insert_ThreeKeysInAnyShape_RotatesToHeightTwoInOrder(int first, int second, int third)
        {
            // Arrange
            var tree = new AvlTree<int, int>();

            // Act
            tree.Insert(first, first);
            tree.Insert(second, second);
            tree.Insert(third, third);

            // Assert
            Assert.Equal(2, tree.Height);
            Assert.Equal([10, 20, 30], tree.GetRange(0, 100));
        }

        [Fact]
        public void Insert_OutOfOrderKeys_RangeComesBackSorted()
        {
            // Arrange
            var random = new Random(7312);
            var keys = Enumerable.Range(1, 2000).OrderBy(_ => random.Next()).ToList();

            // Act
            var tree = Build(keys);

            // Assert
            Assert.Equal(Enumerable.Range(1, 2000), tree.GetRange(1, 2000));
            Assert.True(tree.Height <= 1.44 * Math.Log2(2000) + 1);
        }

        [Fact]
        public void Insert_ALargeLoad_HeightStaysLogarithmic()
        {
            // Arrange
            var tree = new AvlTree<int, int>();

            // Act
            for (var i = 0; i < 100000; i++)
            {
                tree.Insert(i, i);
            }

            // Assert
            Assert.Equal(100000, tree.Count);
            Assert.True(tree.Height <= 1.44 * Math.Log2(100000) + 1);
        }

        [Fact]
        public void GetRange_BothEnds_AreIncluded()
        {
            // Arrange
            var tree = Build(Enumerable.Range(1, 10));

            // Act
            var found = tree.GetRange(3, 7);

            // Assert
            Assert.Equal([3, 4, 5, 6, 7], found);
        }

        [Fact]
        public void GetRange_NothingInside_IsEmpty()
        {
            // Arrange
            var tree = Build([10, 20, 30]);

            // Act
            var between = tree.GetRange(11, 19);
            var beyond = tree.GetRange(31, 99);

            // Assert
            Assert.Empty(between);
            Assert.Empty(beyond);
        }

        [Fact]
        public void GetRange_ABigTree_ReturnsOnlyTheSlice()
        {
            // Arrange
            var tree = Build(Enumerable.Range(0, 50000));

            // Act
            var found = tree.GetRange(25000, 25009);

            // Assert
            Assert.Equal(Enumerable.Range(25000, 10), found);
        }

        [Fact]
        public void GetRange_StartAfterEnd_Throws()
        {
            // Arrange
            var tree = Build([1, 2, 3]);

            // Act
            var act = () => tree.GetRange(3, 1);

            // Assert
            Assert.Throws<ArgumentException>(act);
        }

        [Fact]
        public void Insert_RepeatedKey_KeepsEveryValueInArrivalOrder()
        {
            // Arrange
            var tree = new AvlTree<int, string>();
            tree.Insert(1, "a");
            tree.Insert(2, "first");
            tree.Insert(3, "c");
            var heightBefore = tree.Height;

            // Act
            tree.Insert(2, "second");
            var found = tree.GetRange(1, 3);

            // Assert
            Assert.Equal(["a", "first", "second", "c"], found);
            Assert.Equal(4, tree.Count);
            Assert.Equal(heightBefore, tree.Height);
        }

        [Fact]
        public void GetRange_TimestampKeys_FindsTheReadingsInsideTheWindow()
        {
            // Arrange
            var start = new DateTime(2026, 11, 1, 8, 0, 0, DateTimeKind.Utc);
            var tree = new AvlTree<DateTime, int>();

            for (var i = 0; i < 60; i++)
            {
                tree.Insert(start.AddSeconds(i), i);
            }

            // Act
            var found = tree.GetRange(start.AddSeconds(10), start.AddSeconds(14));

            // Assert
            Assert.Equal([10, 11, 12, 13, 14], found);
        }
    }
}

//.....................................o0oEND OF FILEo0o........................................//
