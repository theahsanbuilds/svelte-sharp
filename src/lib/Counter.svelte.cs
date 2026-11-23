
public partial class Counter
{
    [Parameter]
    public string Title { get; set; } = "My Counter";


    private string Name = "Ahsan";

    private void Increment()
    {
        count++;
    }

    private void Decrement()
    {
        count--;
    }
}
