
<script lang="ts">
  import Counter from '$lib/Counter.svelte';
</script>

<svelte:head>
  <title>SvelteSharp Playground</title>
  <meta name="description" content="Testing C# components in SvelteSharp." />
</svelte:head>

<main>
  <h1>SvelteSharp Playground</h1>
  <p>Testing C# component translation.</p>

  <Counter title="Sales Counter" />
</main>

<style>
  main {
    max-width: 48rem;
    margin: 4rem auto;
    padding: 0 1.5rem;
    font-family: system-ui, sans-serif;
  }
</style>
