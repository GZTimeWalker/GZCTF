import { useLocalStorage } from '@mantine/hooks'
import dayjs from 'dayjs'
import 'dayjs/locale/de'
import 'dayjs/locale/id'
import 'dayjs/locale/ja'
import 'dayjs/locale/ko'
import 'dayjs/locale/ru'
import 'dayjs/locale/zh'
import 'dayjs/locale/zh-tw'
import localizedFormat from 'dayjs/plugin/localizedFormat'
import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import resources from 'virtual:i18next-loader'

dayjs.extend(localizedFormat)

export const LanguageMap = {
  en_US: '🇺🇸 English',
  zh_CN: '🇨🇳 简体中文',
  zh_TW: '🇨🇳 繁體中文',
  ja_JP: '🇯🇵 日本語',
  id_ID: '🇮🇩 Bahasa',
  ko_KR: '🇰🇷 한국어 (wip)',
  ru_RU: '🇷🇺 Русский (wip)',
  de_DE: '🇩🇪 Deutsch (wip)',
}

export const defaultLanguage = 'en_US'
export let apiLanguage: string = defaultLanguage

export type SupportedLanguages = keyof typeof LanguageMap

export const useLanguage = () => {
  const { i18n } = useTranslation()

  const [language, setLanguageInner] = useLocalStorage({
    key: 'language',
    defaultValue: i18n.language,
    getInitialValueInEffect: false,
  })

  useEffect(() => {
    i18n.changeLanguage(language)
    const apiLang = language.replace('_', '-')
    apiLanguage = apiLang
    const pageLang = apiLang.toLowerCase()
    dayjs.locale(pageLang)
    document.documentElement.setAttribute('lang', pageLang)
  }, [language])

  const supportedLanguages = Object.keys(resources) as SupportedLanguages[]

  const setLanguage = (lang: SupportedLanguages) => {
    // check if language is supported
    if (supportedLanguages.includes(lang)) {
      setLanguageInner(lang)
    } else {
      console.warn(`Language ${lang} is not supported, fallback to ${defaultLanguage}`)
      setLanguageInner(defaultLanguage)
    }
  }

  const locale = language.split('_')[0]

  return { language, locale, setLanguage, supportedLanguages }
}

export const normalizeLanguage = (language: string) => language.toUpperCase().replace(/[_-].*/, '')

export const convertLanguage = (language: string): SupportedLanguages => {
  const normalizedLanguage = normalizeLanguage(language)

  const matchedLanguage = Object.keys(LanguageMap).filter(
    (lang) => normalizeLanguage(lang) === normalizedLanguage
  )
  if (matchedLanguage.length > 0) {
    return matchedLanguage.at(0) as SupportedLanguages
  }

  return defaultLanguage
}
