import { FC } from 'react'
import { Text, Stack, Divider, Anchor, Center } from '@mantine/core'
import MainIcon from '@Components/icon/MainIcon'
import { useFooterStyles, useIsMobile, useLogoStyles } from '@Utils/ThemeOverride'
import { useConfig } from '@Utils/useConfig'
import FooterRender from './FooterRender'

const AppFooter: FC = () => {
  const { config } = useConfig()
  const isMobile = useIsMobile()
  const { classes, theme } = useFooterStyles()
  const { classes: logoClasses } = useLogoStyles()

  const copyright = (
    <Text
      size="xs"
      align="center"
      fw={500}
      color="dimmed"
      ff={theme.fontFamilyMonospace}
    >
      Copyright&nbsp;©&nbsp;2022-now&nbsp;
      {isMobile && <br />}
      <Anchor
        href="https://github.com/GZTimeWalker"
        color="dimmed"
        size="sm"
        fw={500}
        sx={{ lineHeight: 1 }}
      >
        @GZTimeWalker
      </Anchor>
      &nbsp;All&nbsp;Rights&nbsp;Reserved.
    </Text>
  )

  return (
    <>
      <div className={classes.spacer} />
      <div className={classes.wrapper}>
        <Center mx="auto" h="100%">
          <Stack spacing="sm" w={isMobile ? '100%' : '80%'}>
            <Stack w="100%" align="center" spacing={2}>
              <MainIcon style={{ maxWidth: isMobile ? 45 : 50, height: 'auto' }} />

              <Text fw="bold" size={isMobile ? 28 : 36}>
                GZ<span className={logoClasses.brand}>::</span>CTF
              </Text>
            </Stack>

            {isMobile ? (
              <>
                {copyright}
                {config.footerInfo && <Divider />}
              </>
            ) : (
              <Divider label={copyright} labelPosition="center" />
            )}

            {config.footerInfo && <FooterRender source={config.footerInfo} />}
          </Stack>
        </Center>
      </div>
    </>
  )
}

export default AppFooter
