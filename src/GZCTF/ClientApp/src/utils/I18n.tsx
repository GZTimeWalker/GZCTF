import translation_en_US from '@Resources/strings.en-US.json'
import translation_ja_JP from '@Resources/strings.ja-JP.json'
import sources from '@Resources/strings.json'
import { Namespace, TOptions } from 'i18next'
import {
  useTranslation as useI18nTranslation,
  Trans as I18nTrans,
  TransProps as I18nTransProps,
} from 'react-i18next'
import { _DefaultNamespace } from 'react-i18next/TransWithoutContext'

export type I18nKey = keyof typeof sources
type Language = {
  translation: {
    [K in I18nKey]: string
  }
}
type Resource = { [K in string]: Language }

export const resources: Resource = {
  'zh-CN': {
    translation: sources,
  },
  en: {
    translation: translation_en_US,
  },
  ja: {
    translation: translation_ja_JP,
  },
}

export const useTranslation = () => {
  const ret = useI18nTranslation()
  return {
    t: (key: I18nKey) => ret.t(key),
    i18n: ret.i18n,
    ready: ret.ready,
  }
}

export function Trans<
  Ns extends Namespace = _DefaultNamespace,
  TOpt extends TOptions = any,
  KPrefix = undefined,
  E = React.HTMLProps<HTMLDivElement>,
>(props: I18nTransProps<I18nKey, Ns, TOpt, KPrefix, E>) {
  return <I18nTrans {...props} />
}
