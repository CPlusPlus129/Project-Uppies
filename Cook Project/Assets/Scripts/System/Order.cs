using System;

public class Order
{
    public string CustomerName;
    public string MealName;
    public Recipe Recipe;

    public override bool Equals(object obj)
    {
        if (obj is not Order other) return false;
        return string.Equals(CustomerName, other.CustomerName, System.StringComparison.InvariantCultureIgnoreCase)
            && string.Equals(MealName, other.MealName, System.StringComparison.InvariantCultureIgnoreCase);
    }

    public override int GetHashCode()
    {
        var comparer = StringComparer.InvariantCultureIgnoreCase;
        int customerHash = CustomerName == null ? 0 : comparer.GetHashCode(CustomerName);
        int mealHash = MealName == null ? 0 : comparer.GetHashCode(MealName);
        return HashCode.Combine(customerHash, mealHash);
    }
}
