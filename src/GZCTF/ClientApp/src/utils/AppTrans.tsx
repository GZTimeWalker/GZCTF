import { Trans } from "react-i18next"
import sources from '../resources/strings.json'
import translation_en_US from '../resources/strings.en-US.json'
import translation_ja_JP from '../resources/strings.ja-JP.json'

type Params<T extends string> = T extends `${string}{{${infer Param}}}${infer Tail}` ? Param | Params<Tail> : never
type InterpolationParams<T extends string> = { [K in Params<T>]: string }
type I18nKey = keyof (typeof sources)
type I18nValue<K extends I18nKey> = InterpolationParams<typeof sources[K]>
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

const AppTrans = <K extends I18nKey>(props: { i18nKey: K, values?: I18nValue<K> }) => {
  return <Trans i18nKey={props.i18nKey} values={props.values}></Trans>
}

export default AppTrans
