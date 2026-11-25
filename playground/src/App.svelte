<script>
import CodeEditor from './lib/CodeEditor.svelte';
    import { onMount } from 'svelte';
let saveStatus = 'Saved locally';
    let compiler = null;
    let compilerReady = false;
    let compiling = false;

    let activeMobileEditor = 'csharp';
    let activeOutputTab = 'generated';

    let status = 'Initializing compiler';
    let statusType = 'loading';

    let csharpCode = `public class Playground
{
    private string message = "Hello from C#";
    private int count = 0;
}`;

    let svelteCode = `<h1>{message}</h1>

<p>Counter: {count}</p>

<button onclick="Increment">
    Click me
</button>`;

    let generatedCode = '';
    let diagnostics = 'No errors.';
    let lastCompiled = '';

    const defaultCsharp = csharpCode;
    const defaultSvelte = svelteCode;

    $: csharpLines = csharpCode.split('\n').length;
    $: svelteLines = svelteCode.split('\n').length;

   onMount(() => {
    try {
        const savedDraft = localStorage.getItem('svelte-sharp-playground');

        if (savedDraft) {
            const draft = JSON.parse(savedDraft);

            if (typeof draft.csharpCode === 'string') {
                csharpCode = draft.csharpCode;
            }

            if (typeof draft.svelteCode === 'string') {
                svelteCode = draft.svelteCode;
            }
        }
    } catch (error) {
        console.error('Could not restore playground draft:', error);
    }

    initializeCompiler();
});
function saveDraft() {
    try {
        localStorage.setItem(
            'svelte-sharp-playground',
            JSON.stringify({
                csharpCode,
                svelteCode
            })
        );

        saveStatus = 'Saved locally';
    } catch (error) {
        saveStatus = 'Could not save';
        console.error(error);
    }
}

function updateCsharp(value) {
    csharpCode = value;
    saveDraft();
}

function updateSvelte(value) {
    svelteCode = value;
    saveDraft();
}

    async function initializeCompiler() {
        try {
            status = 'Loading .NET WebAssembly...';
            statusType = 'loading';

            const compilerUrl = new URL(
                `${import.meta.env.BASE_URL}compiler/main.js`,
                document.baseURI
            ).href;

            const compilerModule = await import(
                /* @vite-ignore */ compilerUrl
            );

            if (typeof compilerModule.translate !== 'function') {
                throw new Error(
                    'SvelteSharp compiler export was not found.'
                );
            }

            compiler = compilerModule;
            compilerReady = true;

            status = 'Compiler ready';
            statusType = 'success';

            diagnostics = 'WASM compiler initialized successfully.';

        } catch (error) {
            compilerReady = false;
            status = 'Compiler initialization failed';
            statusType = 'error';
            diagnostics = formatError(error);
        }
    }

    function runCompiler() {
        if (!compiler || compiling) return;

        try {
            compiling = true;
            status = 'Compiling component...';
            statusType = 'loading';

            diagnostics = 'No errors.';

            const result = compiler.translate(
                svelteCode,
                csharpCode
            );

            generatedCode = result;
            lastCompiled = new Date().toLocaleTimeString();

            status = 'Translation successful';
            statusType = 'success';

            activeOutputTab = 'generated';

        } catch (error) {
            generatedCode = '';
            diagnostics = formatError(error);

            status = 'Compilation failed';
            statusType = 'error';

            activeOutputTab = 'diagnostics';

        } finally {
            compiling = false;
        }
    }

    function formatError(error) {
        return error?.stack || error?.message || String(error);
    }

    async function copyOutput() {
        if (!generatedCode) return;

        try {
            await navigator.clipboard.writeText(generatedCode);
            status = 'Output copied';
            statusType = 'success';
        } catch {
            status = 'Clipboard access unavailable';
            statusType = 'error';
        }
    }
function resetEditor() {
    csharpCode = defaultCsharp;
    svelteCode = defaultSvelte;

    generatedCode = '';
    diagnostics = 'Editor reset. Ready to compile.';

    saveDraft();

    status = 'Editor reset';
    statusType = 'success';
}

    function handleKeydown(event) {
        if ((event.ctrlKey || event.metaKey) && event.key === 'Enter') {
            event.preventDefault();
            runCompiler();
        }
    }

    function selectMobileEditor(editor) {
        activeMobileEditor = editor;
    }
</script>

<svelte:window onkeydown={handleKeydown} />

<div class="app-shell">

    <!-- Sidebar -->

    <aside class="sidebar">
        <div class="brand">
            <div class="brand-icon">
                <svg viewBox="0 0 24 24" fill="none">
                    <path d="M8 5L2 12L8 19M16 5L22 12L16 19M14 3L10 21"
                        stroke="currentColor"
                        stroke-width="2"
                        stroke-linecap="round"
                        stroke-linejoin="round"/>
                </svg>
            </div>

            <div class="brand-text">
                <strong>SvelteSharp</strong>
                <span>Studio</span>
            </div>
        </div>

        <div class="sidebar-section">
            <span class="sidebar-label">WORKSPACE</span>

            <button class="nav-item active">
                <span>▣</span>
                Playground
            </button>

            <button class="nav-item" onclick={resetEditor}>
                <span>↺</span>
                Reset project
            </button>
        </div>

        <div class="sidebar-section">
            <span class="sidebar-label">COMPILER</span>

            <div class="compiler-card">
                <div class="compiler-heading">
                    <span class="status-dot {compilerReady ? 'online' : ''}"></span>
                    <strong>.NET WebAssembly</strong>
                </div>

                <span class="compiler-description">
                    {compilerReady ? 'Running in your browser' : 'Initializing runtime...'}
                </span>

                <div class="compiler-version">SvelteSharp · net9.0</div>
            </div>
        </div>

        <div class="sidebar-footer">
            <div class="avatar">SS</div>

            <div>
                <strong>SvelteSharp</strong>
                <span>Browser playground</span>
            </div>
        </div>
    </aside>

    <!-- Main content -->

    <main class="main-content">

        <header class="topbar">
            <div class="breadcrumb">
                <span>Workspace</span>
                <span class="breadcrumb-divider">/</span>
                <strong>Playground</strong>
            </div>

            <div class="topbar-actions">
                <div class="runtime-badge">
                    <span class="status-dot online"></span>
                    Browser runtime
                </div>

                <button class="icon-button" title="Reset editor" onclick={resetEditor}>
                    ↺
                </button>
            </div>
        </header>

        <section class="page-content">

            <div class="welcome-section">
                <div>
                    <div class="eyebrow">DEVELOPMENT ENVIRONMENT</div>

                    <h1>Playground</h1>

                    <p>
                        Write C# components, translate them into Svelte 5,
                        and experiment directly in your browser.
                    </p>
                </div>

                <button
                    class="run-button"
                    onclick={runCompiler}
                    disabled={!compilerReady || compiling}
                >
                    {#if compiling}
                        <span class="spinner"></span>
                        Compiling
                    {:else}
                        <span>▶</span>
                        Run component
                    {/if}
                </button>
            </div>

            <!-- Status bar -->

            <div class="status-bar">
                <div class="status-left">
                    <span class="status-dot {statusType}"></span>
                    <span>{status}</span>
                </div>

               <div class="status-right">
    <span>{saveStatus}</span>
    <span>Ctrl + Enter to run</span>

    {#if lastCompiled}
        <span>Last run: {lastCompiled}</span>
    {/if}
</div>
            </div>

            <!-- Editors -->

            <section class="workspace">

                <div class="editor-toolbar">
                    <div class="editor-tabs">
                        <button
                            class:active={activeMobileEditor === 'csharp'}
                            onclick={() => selectMobileEditor('csharp')}
                        >
                            <span class="language-icon csharp-icon">C#</span>
                            C# Component
                        </button>

                        <button
                            class:active={activeMobileEditor === 'svelte'}
                            onclick={() => selectMobileEditor('svelte')}
                        >
                            <span class="language-icon svelte-icon">S</span>
                            Svelte Markup
                        </button>
                    </div>

                    <span class="editor-hint">Editable source</span>
                </div>

                <div class="editor-grid">

               <div class="editor-panel"
    class:mobile-active={activeMobileEditor === 'csharp'}>

    <div class="panel-heading">
        <div>
            <span class="language-icon csharp-icon">C#</span>
            <strong>Component.cs</strong>
        </div>

        <span class="file-type">C#</span>
    </div>

    <div class="code-editor">
        <CodeEditor
            value={csharpCode}
            language="csharp"
            onChange={updateCsharp}
        />
    </div>

    <div class="editor-footer">
        <span>{csharpCode.split('\n').length} lines</span>
        <span>C#</span>
    </div>
</div>

                  <div class="editor-panel"
    class:mobile-active={activeMobileEditor === 'svelte'}>

    <div class="panel-heading">
        <div>
            <span class="language-icon svelte-icon">S</span>
            <strong>Playground.svelte</strong>
        </div>

        <span class="file-type">Svelte</span>
    </div>

    <div class="code-editor">
        <CodeEditor
            value={svelteCode}
            language="svelte"
            onChange={updateSvelte}
        />
    </div>

    <div class="editor-footer">
        <span>{svelteCode.split('\n').length} lines</span>
        <span>Svelte 5</span>
    </div>
</div>

                </div>
            </section>

            <!-- Output -->

            <section class="output-section">

                <div class="output-header">
                    <div class="output-tabs">
                        <button
                            class:active={activeOutputTab === 'generated'}
                            onclick={() => activeOutputTab = 'generated'}
                        >
                            <span>⌘</span>
                            Generated Svelte
                        </button>

                        <button
                            class:active={activeOutputTab === 'diagnostics'}
                            onclick={() => activeOutputTab = 'diagnostics'}
                        >
                            <span>ⓘ</span>
                            Diagnostics
                        </button>
                    </div>

                    {#if activeOutputTab === 'generated'}
                        <button
                            class="copy-button"
                            onclick={copyOutput}
                            disabled={!generatedCode}
                        >
                            ▢ Copy output
                        </button>
                    {/if}
                </div>

                {#if activeOutputTab === 'generated'}
                    <div class="output-content">
                        {#if generatedCode}
                            <pre><code>{generatedCode}</code></pre>
                        {:else}
                            <div class="empty-state">
                                <div class="empty-icon">⌘</div>
                                <strong>No generated output yet</strong>
                                <span>Click Run component to translate your C# component.</span>
                            </div>
                        {/if}
                    </div>
                {:else}
                    <div class="diagnostics-content">
                        <pre>{diagnostics}</pre>
                    </div>
                {/if}

                <div class="output-footer">
                    <span>Generated by SvelteSharp Compiler</span>
                    <span>In-memory translation</span>
                </div>

            </section>

            <footer class="page-footer">
                <span>Built with Svelte 5 and .NET WebAssembly</span>
                <span>SvelteSharp Playground</span>
            </footer>

        </section>
    </main>
</div>
