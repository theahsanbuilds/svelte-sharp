<script>
    import { onMount } from 'svelte';

    import { basicSetup } from 'codemirror';
    import { EditorView } from '@codemirror/view';
    import { oneDark } from '@codemirror/theme-one-dark';

    import { csharp } from '@replit/codemirror-lang-csharp';
    import { svelte } from 'codemirror-lang-svelte';

    export let value = '';
    export let language = 'csharp';
    export let onChange = () => {};

    let editorElement;
    let editorView;

    const editorTheme = EditorView.theme({
        '&': {
            height: '100%',
            backgroundColor: '#0d1017',
            color: '#d6d9e2',
            fontSize: '12px'
        },
        '.cm-scroller': {
            overflow: 'auto',
            fontFamily: "'JetBrains Mono', monospace"
        },
        '.cm-gutters': {
            backgroundColor: '#0d1017',
            color: '#535b6b',
            border: 'none'
        },
        '.cm-activeLineGutter': {
            backgroundColor: '#1c2230'
        },
        '.cm-activeLine': {
            backgroundColor: '#ffffff08'
        },
        '&.cm-focused': {
            outline: 'none'
        }
    });

    onMount(() => {
        const languageExtension =
            language === 'csharp' ? csharp() : svelte();

        editorView = new EditorView({
            doc: value,
            extensions: [
                basicSetup,
                oneDark,
                languageExtension,
                editorTheme,

                EditorView.updateListener.of((update) => {
                    if (update.docChanged) {
                        onChange(update.state.doc.toString());
                    }
                })
            ],
            parent: editorElement
        });

        return () => {
            editorView?.destroy();
        };
    });

    // Synchronize external changes, such as resetting the editor.
    $: if (
        editorView &&
        editorView.state.doc.toString() !== value
    ) {
        editorView.dispatch({
            changes: {
                from: 0,
                to: editorView.state.doc.length,
                insert: value
            }
        });
    }
</script>

<div class="code-editor-host" bind:this={editorElement}></div>

<style>
    .code-editor-host {
        height: 100%;
        min-height: 300px;
        overflow: hidden;
    }

    .code-editor-host :global(.cm-editor) {
        height: 100%;
    }
</style>
