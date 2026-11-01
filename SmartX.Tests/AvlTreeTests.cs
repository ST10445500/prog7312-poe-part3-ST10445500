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

    }
}

//.....................................o0oEND OF FILEo0o........................................//
