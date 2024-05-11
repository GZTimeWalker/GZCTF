import { Anchor, Center, Divider, Stack, Text } from '@mantine/core'
import { FC } from 'react'
import FooterRender from '@Components/FooterRender'
import MainIcon from '@Components/icon/MainIcon'
import { useIsMobile } from '@Utils/ThemeOverride'
import { useConfig } from '@Utils/useConfig'
import classes from '@Styles/AppFooter.module.css'
import iconClasses from '@Styles/Icon.module.css'

const AppFooter: FC = () => {
  const { config } = useConfig()
  const isMobile = useIsMobile()

  const copyright = (
    <Text size="xs" ta="center" fw={500} c="dimmed" ff="monospace">
      Copyright&nbsp;©&nbsp;2022-now&nbsp;
      {isMobile && <br />}
      <Anchor href="https://github.com/GZTimeWalker" c="dimmed" size="sm" fw={500} lh={1}>
        @GZTimeWalker
      </Anchor>
      ,&nbsp;All&nbsp;Rights&nbsp;Reserved.
    </Text>
  )

  return (
    <>
      <div className={classes.spacer} />
      <div className={classes.wrapper}>
        <Center mx="auto" h="100%">
          <Stack gap="sm" w={isMobile ? '100%' : '80%'}>
            <Stack w="100%" align="center" gap={2}>
              <MainIcon style={{ maxWidth: isMobile ? '3rem' : '4rem', height: 'auto' }} />
              <Text fw="bold" size={isMobile ? '2rem' : '2.5rem'}>
                GZ<span className={iconClasses.front}>::</span>CTF
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
