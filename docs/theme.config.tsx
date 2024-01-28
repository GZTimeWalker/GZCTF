import React from "react";
import LogoHeader from "@Components/LogoHeader";
import type { DocsThemeConfig } from "nextra-theme-docs";

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
  i18n: [{ locale: "zh", text: "中文" }, { locale: "ja", text: "日本語" }],
  feedback: {
    content: "文档有问题？欢迎反馈 →",
  },
  editLink: {
    text: "编辑此页面",
  },
  toc: {
    title: "此页内容",
  },
  footer: {
    text: "©2022-present By GZTimeWalker"
  },
};

export default config;
