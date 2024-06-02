import React from "react";
import LogoHeader from "@Components/LogoHeader";
import type { DocsThemeConfig } from "nextra-theme-docs";
import { useRouter } from "next/router";

const Feedback: React.FC = () => {
  const { locale } = useRouter();

  switch (locale) {
    case "zh":
      return "文档有问题？欢迎反馈 →";
    case "ja":
      return "フィードバックはこちら →";
    case "en":
      return "Feedback →";
    default:
      return "Feedback →";
  }
};

const EditLink: React.FC = () => {
  const { locale } = useRouter();

  switch (locale) {
    case "zh":
      return "编辑此页面";
    case "ja":
      return "ページの編集";
    case "en":
      return "Edit this page";
    default:
      return "Edit this page";
  }
};

const Toc: React.FC = () => {
  const { locale } = useRouter();

  switch (locale) {
    case "zh":
      return "此页内容";
    case "ja":
      return "ページの内容";
    case "en":
      return "Page content";
    default:
      return "Page content";
  }
};

const config: DocsThemeConfig = {
  docsRepositoryBase: "https://github.com/GZTimeWalker/GZCTF/tree/main/docs",
  useNextSeoProps() {
    return {
      titleTemplate: "%s - GZ::CTF Docs",
    };
  },
  head: <link rel="icon" type="image/png" href="/favicon.png" />,
  logo: LogoHeader,
  project: {
    link: "https://github.com/GZTimeWalker/GZCTF",
  },
  search: {
    component: null,
  },
  i18n: [
    { locale: "zh", text: "中文" },
    { locale: "en", text: "English" },
    { locale: "ja", text: "日本語" },
  ],
  feedback: {
    content: Feedback,
  },
  editLink: {
    text: EditLink,
  },
  toc: {
    title: Toc,
  },
  footer: {
    text: "©2022-present By GZTimeWalker",
  },
};

export default config;
