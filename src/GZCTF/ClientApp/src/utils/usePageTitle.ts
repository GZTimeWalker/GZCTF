import { useDocumentTitle } from '@mantine/hooks'
import { useConfig } from '@Utils/useConfig'

export const usePageTitle = (title?: string) => {
  const { config, error } = useConfig()

  const platform = error ? 'GZ::CTF' : `${config?.title ?? 'GZ'}::CTF`

  useDocumentTitle(
    typeof title === 'string' && title.trim().length > 0 ? `${title} - ${platform}` : platform
  )
}
