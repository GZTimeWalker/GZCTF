import {
  AppShell,
  Box,
  LoadingOverlay,
  Stack,
  useMantineColorScheme,
  useMantineTheme,
} from '@mantine/core'
import React, { FC } from 'react'
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
  const { colorScheme } = useMantineColorScheme()
  const { user } = useUser()

  return (
    <WithWiderScreen minWidth={minWidth}>
      <Watermark
        text={user?.userId ?? ''}
        textColor={colorScheme === 'dark' ? theme.colors.gray[3] : theme.colors.gray[7]}
        rotate={-12}
        textSize={14}
        gutter={22}
        opacity={colorScheme === 'dark' ? 0.016 : 0.024}
      >
        <AppShell
          padding={0}
          styles={{
            body: {
              overflow: 'hidden',
            },
          }}
        >
          <AppNavbar />
          <AppHeader />
          <AppShell.Main>
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
                  colorScheme === 'dark' ? theme.colors.gray[7] : theme.colors.light[2],
              }}
            >
              <LoadingOverlay
                visible={isLoading ?? false}
                opacity={1}
                c={colorScheme === 'dark' ? theme.colors.gray[7] : theme.colors.light[2]}
              />
              <Box
                sx={(_, u) => ({
                  width: width ?? '80%',
                  zIndex: 20,

                  [u.smallerThan('xs')]: {
                    width: '97%',
                  },
                })}
              >
                {children}
              </Box>
            </Stack>
            {withFooter && <AppFooter />}
          </AppShell.Main>
        </AppShell>
      </Watermark>
    </WithWiderScreen>
  )
}

export default WithNavBar
