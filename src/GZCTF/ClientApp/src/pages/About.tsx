import { Anchor, Badge, Center, Group, HoverCard, Stack, Text, Title, useMantineTheme } from '@mantine/core'
import { FC } from 'react'
import { useTranslation } from 'react-i18next'
import { WithNavBar } from '@Components/WithNavbar'
import { MainIcon } from '@Components/icon/MainIcon'
import { useConfig, ValidatedRepoMeta } from '@Hooks/useConfig'
import { usePageTitle } from '@Hooks/usePageTitle'
import classes from '@Styles/About.module.css'
import logoClasses from '@Styles/LogoHeader.module.css'
import misc from '@Styles/Misc.module.css'

const About: FC = () => {
  const { config } = useConfig()
  const { repo, valid, rawTag: tag, sha, buildTime } = ValidatedRepoMeta()
  const { t } = useTranslation()
  const theme = useMantineTheme()
  const shortSha = `#${sha.substring(0, 8)}`

  usePageTitle(t('common.title.about'))

  return (
    <WithNavBar>
      <Stack justify="space-between" h="calc(100vh - 16px)">
        <Center h="calc(100vh - 16px)">
          <Title order={2} className={classes.watermark}>
            GZ::CTF
          </Title>
          <Text className={classes.bio}>
            &gt; {config?.slogan ?? t('common.content.about.slogan')}
            <Text span className={classes.blink}>
              _
            </Text>
          </Text>
        </Center>
        <Group justify="right">
          <HoverCard shadow="md" position="top-end" withArrow openDelay={200} closeDelay={400}>
            <HoverCard.Target>
              <Badge onClick={() => window.open(repo, '_blank')} className={misc.cPointer} size="lg" variant="outline">
                © 2022-Now GZTime {valid ? shortSha : ''}
              </Badge>
            </HoverCard.Target>
            <HoverCard.Dropdown>
              <Stack>
                <Group>
                  <MainIcon size="60px" />
                  <Stack gap="xs">
                    <Title ml="-20px" mb="-5px" className={classes.title}>
                      GZ<span className={logoClasses.brand}>::</span>CTF
                    </Title>
                    <Group ml="-18px" mt="-5px">
                      <Anchor href="https://github.com/GZTimeWalker" c="dimmed" size="sm" fw={500} lh={1}>
                        @GZTimeWalker
                      </Anchor>
                      <Badge
                        variant={valid ? 'light' : 'filled'}
                        color={valid ? theme.primaryColor : 'alert'}
                        size="xs"
                      >
                        {valid ? `${tag}${shortSha}` : 'UNOFFICIAL'}
                      </Badge>
                    </Group>
                  </Stack>
                </Group>
                <Group gap="xs">
                  <Text size="xs" fw={500} c="dimmed" ff="monospace">
                    {valid
                      ? `Built at ${buildTime.format('YYYY-MM-DDTHH:mm:ssZ')}`
                      : 'This release is not officially built'}
                  </Text>
                </Group>
              </Stack>
            </HoverCard.Dropdown>
          </HoverCard>
        </Group>
      </Stack>
    </WithNavBar>
  )
}

export default About
