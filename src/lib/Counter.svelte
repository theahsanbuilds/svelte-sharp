@code{
    private int count = a1;
}
<div class="counter">
    <h2>{Title}</h2>

    <p>Count: {count}</p>

    <button onclick={Increment}>+</button>
    <button onclick={Decrement}>-</button>

    <hr />

    <h3>Two-way Binding Test</h3>

    <input bind:value={Name} />

    <p>Hello {Name}</p>
</div>
