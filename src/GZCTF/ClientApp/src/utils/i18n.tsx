import sources from '../resources/strings.json'
import translation_en_US from '../resources/strings.en-US.json'
import translation_ja_JP from '../resources/strings.ja-JP.json'

type I18nKey = keyof (typeof sources)
type Language = {
  translation:
  {
    [K in I18nKey]: string
  }
}
type Resource = { [K in string]: Language }

export const resources: Resource =
{
  "zh-CN": {
    translation: sources
  },
  "en": {
    translation: translation_en_US
  },
  "ja": {
    translation: translation_ja_JP
  }
}

const i18nKeyOf = (key: I18nKey) => key;

export default i18nKeyOf;
