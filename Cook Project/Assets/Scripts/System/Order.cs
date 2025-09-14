public class Order
{
    public string CustomerName;
    public string MealName;

    public override bool Equals(object obj)
    {
        if (obj is not Order other) return false;
        return string.Equals(CustomerName, other.CustomerName, System.StringComparison.InvariantCultureIgnoreCase)
            && string.Equals(MealName, other.MealName, System.StringComparison.InvariantCultureIgnoreCase);
    }

    public override int GetHashCode()
    {
        return (CustomerName, MealName).GetHashCode();
    }
}