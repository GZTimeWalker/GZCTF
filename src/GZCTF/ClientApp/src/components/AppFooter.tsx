import { FC } from 'react'
import { Text, Stack, Divider, Anchor, Center } from '@mantine/core'
import MainIcon from '@Components/icon/MainIcon'
import { useFooterStyles, useIsMobile, useLogoStyles } from '@Utils/ThemeOverride'
import { useConfig } from '@Utils/useConfig'

const AppFooter: FC = () => {
  const { config } = useConfig()
  const isMobile = useIsMobile()
  const { classes } = useFooterStyles()
  const { classes: logoClasses } = useLogoStyles()

  const copyright = (
    <Text
      size="xs"
      align="center"
      weight={500}
      color="dimmed"
      sx={(theme) => ({ fontFamily: theme.fontFamilyMonospace })}
    >
      Copyright&nbsp;©&nbsp;2022-now&nbsp;
      {isMobile && <br />}
      <Anchor
        href="https://github.com/GZTimeWalker"
        color="dimmed"
        size="sm"
        weight={500}
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

              <Text weight="bold" size={isMobile ? 28 : 36}>
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

            {config.footerInfo && (
              <Text align="center" size="sm" color="dimmed">
                {config.footerInfo}
              </Text>
            )}
          </Stack>
        </Center>
      </div>
    </>
  )
}

export default AppFooter
