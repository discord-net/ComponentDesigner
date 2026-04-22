import SyntaxHighlighter from "react-syntax-highlighter";
import "./markdown.scss";

import MD, {
  type BoldProps,
  type CodeBlockProps,
  type CodeProps,
  type EmojiProps,
  type HeadingProps,
  type LinkProps,
  type ListItemProps,
  type ListProps,
  type ParagraphProps,
  type QuoteProps,
  type Renderers,
  type SmallProps,
  type TextProps,
} from "@discord/markdown-react";
import { useState } from "react";
import classNames from "classnames";

const bold = (props: BoldProps) => <b>{props.children}</b>;

const italic = (props: BoldProps) => <i>{props.children}</i>;

const underline = (props: BoldProps) => <u>{props.children}</u>;
const strikethrough = (props: BoldProps) => <s>{props.children}</s>;
const spoiler = (props: BoldProps) => {
  const [expanded, setExpanded] = useState(false);

  return (
    <span
      onClick={() => {
        if(!expanded) setExpanded(true);
      }}
      className={classNames(
        "spoiler",
        { "spoiler-obscured": !expanded },
        { "spoiler-shown": expanded },
      )}
    >
      {props.children}
    </span>
  );
};
const emoji = (props: EmojiProps) => <span>EMOJI HERE</span>;
const timestamp = () => <span>TIMESTAMP</span>;
const mention = () => <span>MENTION</span>;
const link = (props: LinkProps) => {
  if (props.type === "normal") {
    return <a href={props.value.url}>{props.children}</a>;
  }

  return <span>MENTION LINK</span>;
};

const code = (props: CodeProps) => (
  <code className="inline">{props.children}</code>
);

const code_block = (props: CodeBlockProps) => {
  return <code className="block">{props.content}</code>;
};

const heading = (props: HeadingProps) => {
  if (props.level === 1) return <h1>{props.children}</h1>;
  if (props.level === 2) return <h2>{props.children}</h2>;
  if (props.level === 3) return <h3>{props.children}</h3>;
  return <span>{props.children}</span>;
};
const list = (props: ListProps) => {
  if (props.type === "ordered") {
    return <ol>{props.children}</ol>;
  }

  return <ul>{props.children}</ul>;
};
const quote = (props: QuoteProps) => (
  <div className="blockquoteContainer">
    <div className="blockquoteDivider" />
    <blockquote>{props.children}</blockquote>
  </div>
);
const small = (props: SmallProps) => <small>{props.children}</small>;
const text = (props: TextProps) => <span>{props.children}</span>;
const paragraph = (props: ParagraphProps) => <p>{props.children}</p>;
const empty = () => <span></span>;
const listItem = (props: ListItemProps) => {
  return <li>{props.children}</li>;
};

const renderers: Renderers = {
  bold,
  italic,
  underline,
  strikethrough,
  spoiler,
  emoji,
  timestamp,
  mention,
  link,
  code,
  code_block,
  heading,
  list,
  quote,
  small,
  text,
  paragraph,
  empty,
  listItem,
};

export default function Markdown({ text }: { text: string }) {
  return (
    <div className="markdown-container">
      <MD content={text} renderers={renderers} />
    </div>
  );
}
