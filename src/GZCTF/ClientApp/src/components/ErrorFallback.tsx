import { Button, Center, Text, Stack, Title, useMantineTheme, Textarea, Group } from '@mantine/core'
import { FC } from 'react'
import { FallbackProps } from 'react-error-boundary'
import { useTranslation } from 'react-i18next'
import { useIsMobile } from '@Utils/ThemeOverride'
import { clearLocalCache } from '@Utils/useConfig'

const ErrorFallback: FC<FallbackProps> = ({ error, resetErrorBoundary }: FallbackProps) => {
  const theme = useMantineTheme()
  const { t } = useTranslation()
  const isMobile = useIsMobile()

  return (
    <Center h="100vh" px="15%">
      <Stack maw="60rem" miw={isMobile ? '92vw' : '30rem'} w="70%" gap="sm">
        <Title fw="bold" order={1} c={theme.primaryColor}>
          # {t('common.error.encountered')}
        </Title>
        <Text fz="lg" fw={500}>
          {error.message}
        </Text>
        <Textarea
          value={error.stack}
          autosize
          minRows={12}
          maxRows={20}
          tabIndex={-1}
          styles={{
            input: {
              fontFamily: theme.fontFamilyMonospace,
              fontSize: theme.fontSizes.sm,
            },
          }}
        />
        <Text ta="center" size="sm" fw="bold" c="dimmed">
          &gt;&gt;&gt; {t('common.content.report_error')}&lt;&lt;&lt;
        </Text>
        <Group grow>
          <Button variant="outline" onClick={resetErrorBoundary}>
            {t('common.button.try_again')}
          </Button>
          <Button variant="outline" onClick={clearLocalCache}>
            {t('common.tab.account.clean_cache')}
          </Button>
        </Group>
      </Stack>
    </Center>
  )
}

export default ErrorFallback
