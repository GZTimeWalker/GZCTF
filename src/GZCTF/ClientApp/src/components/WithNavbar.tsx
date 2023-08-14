import React, { FC } from 'react'
import { AppShell, Box, LoadingOverlay, Stack, useMantineTheme } from '@mantine/core'
import AppFooter from '@Components/AppFooter'
import AppHeader from '@Components/AppHeader'
import AppNavbar from '@Components/AppNavbar'
import Watermark from '@Components/Watermark'
import WithWiderScreen from '@Components/WithWiderScreen'
import { useUser } from '@Utils/useUser'

interface WithNavBarProps extends React.PropsWithChildren {
  width?: string
  minWidth?: number
  isLoading?: boolean
  withFooter?: boolean
}

const WithNavBar: FC<WithNavBarProps> = ({
  children,
  width,
  isLoading,
  minWidth,
  withFooter = false,
}) => {
  const theme = useMantineTheme()
  const { user } = useUser()

  return (
    <WithWiderScreen minWidth={minWidth}>
      <Watermark
        text={user?.userId ?? ''}
        textColor={theme.colorScheme === 'dark' ? theme.colors.gray[3] : theme.colors.gray[7]}
        rotate={-9}
        textSize={15}
        gutter={20}
        opacity={theme.colorScheme === 'dark' ? 0.012 : 0.024}
        fontFamily="JetBrains Mono"
      >
        <AppShell
          padding={0}
          fixed
          navbar={<AppNavbar />}
          header={<AppHeader />}
          styles={{
            body: {
              overflow: 'hidden',
            },
          }}
        >
          <Stack
            w="100%"
            mih="100vh"
            pb={withFooter ? 'xl' : 0}
            pos="relative"
            align="center"
            style={{
              zIndex: 10,
              boxShadow: theme.shadows.sm,
              backgroundColor:
                theme.colorScheme === 'dark' ? theme.colors.gray[7] : theme.colors.white[2],
            }}
          >
            <LoadingOverlay
              visible={isLoading ?? false}
              overlayOpacity={1}
              overlayColor={
                theme.colorScheme === 'dark' ? theme.colors.gray[7] : theme.colors.white[2]
              }
            />
            <Box
              sx={(theme) => ({
                width: width ?? '80%',
                zIndex: 20,

                [theme.fn.smallerThan('xs')]: {
                  width: '97%',
                },
              })}
            >
              {children}
            </Box>
          </Stack>
          {withFooter && <AppFooter />}
        </AppShell>
      </Watermark>
    </WithWiderScreen>
  )
}

export default WithNavBar
