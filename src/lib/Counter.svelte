@code {
    private int count = 10;
    private string Status = "Pending";
    private string[] Products = ["Apple", "Orange", "Mango"];

    private void Increment()
    {
        count++;
    }
}

<div>
    <h1>Counter: @count</h1>

    <button @onclick="Increment">Increase</button>

    @if (count > 10)
    {
        <p>Greater than 10</p>
    }
    else if (count == 10)
    {
        <p>Exactly 10</p>
    }
    else
    {
        <p>Less than 10</p>
    }

    <hr />

    <h2>Products</h2>

    @foreach (var product in Products)
    {
        <p>@product</p>
    }

    <hr />

    <h2>For loop</h2>

    @for (int i = 0; i < 5; i++)
    {
        <p>Number: @i</p>
    }

    <hr />

    <h2>Switch</h2>

    @switch (Status)
    {
        case "Pending":
            <p>Waiting for approval</p>
            break;

        case "Approved":
            <p>Approved</p>
            break;

        default:
            <p>Unknown status</p>
            break;
    }
</div>