import { Stack, Text, Title } from '@mantine/core'
import { useViewportSize } from '@mantine/hooks'
import { FC } from 'react'
import { useTranslation } from 'react-i18next'
import IconWiderScreenRequired from '@Components/icon/WiderScreenRequiredIcon'

interface WithWiderScreenProps extends React.PropsWithChildren {
  minWidth?: number
}

const WithWiderScreen: FC<WithWiderScreenProps> = ({ children, minWidth = 1080 }) => {
  const view = useViewportSize()

  const { t } = useTranslation()

  const tooSmall = minWidth > 0 && view.width > 0 && view.width < minWidth

  return tooSmall ? (
    <Stack gap={0} align="center" justify="center" h="calc(100vh - 32px)">
      <IconWiderScreenRequired />
      <Title order={1} c="brand" fw="lighter">
        {t('common.content.wider.title')}
      </Title>
      <Text fw="bold">{t('common.content.wider.text')}</Text>
    </Stack>
  ) : (
    <>{children}</>
  )
}

export default WithWiderScreen
