export default {
  docsRepositoryBase: "https://github.com/GZTimeWalker/GZCTF/tree/main/docs",
  useNextSeoProps() {
    return {
      titleTemplate: "%s - GZCTF",
    };
  },
  head: (
    <>
      <link
        rel="apple-touch-icon"
        sizes="180x180"
        href="/apple-touch-icon.png"
      />
      <link
        rel="icon"
        type="image/png"
        sizes="32x32"
        href="/favicon-32x32.png"
      />
      <link
        rel="icon"
        type="image/png"
        sizes="16x16"
        href="/favicon-16x16.png"
      />
    </>
  ),
  logo: (
    <div style={{ display: "flex", alignItems: "center", gap: "8px" }}>
      <img src="/logo.svg" width="24px" />
      GZCTF
    </div>
  ),
  project: {
    link: "https://github.com/GZTimeWalker/GZCTF",
  },
  search: {
    component: null,
  },
  i18n: [
    // { locale: "en", text: "English" },
    { locale: "zh", text: "中文" },
  ],
  feedback: {
    content: "文档有问题？欢迎反馈 →",
  },
  editLink: {
    text: "编辑此页面",
  },
  footer: {
    text: (
      <div style={{ width: "100%", textAlign: "center" }}>
        ©2022-present By GZTimeWalker
      </div>
    ),
  },
};
