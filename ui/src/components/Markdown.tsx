import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';

/**
 * Note bodies are markdown. Raw HTML stays disabled (react-markdown's default),
 * so nothing pasted into a note can inject markup — which is also why there is
 * no sanitizer here.
 *
 * remark-gfm is what turns a bare URL into a link, on top of tables,
 * strikethrough and task lists.
 */
export default function Markdown({ children }: { children: string }) {
  return (
    <div className="markdown">
      <ReactMarkdown
        remarkPlugins={[remarkGfm]}
        components={{
          // Links are external by definition here — the app has no routes.
          a: ({ href, children: label }) => (
            <a href={href} target="_blank" rel="noreferrer">
              {label}
            </a>
          ),
        }}
      >
        {children}
      </ReactMarkdown>
    </div>
  );
}
