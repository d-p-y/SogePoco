namespace SogePoco.Impl.Extensions;

public static class ListExtensions {
    public static List<T> RemoveFirstItems<T>(this List<T> self, int count) {
        while (count > 0) {
            self.RemoveAt(0);
            count--;
        }

        return self;
    }
    
    public static List<T> FluentAdd<T>(this List<T> self, T itmToAdd) {
        self.Add(itmToAdd);
        return self;
    }
}
