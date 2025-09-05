import { Anchor, Center, Group, Stack, Text, Title, useMantineTheme, Flex, Paper, Badge, Card } from '@mantine/core'
import { mdiScaleBalance, mdiFileDocumentOutline, mdiGithub, mdiTag, mdiAccountGroup, mdiLink } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC } from 'react'
import { useTranslation } from 'react-i18next'
import contributorsData from 'virtual:contributors'
import { WithNavBar } from '@Components/WithNavbar'
import { MainIcon } from '@Components/icon/MainIcon'
import { useConfig, ValidatedRepoMeta } from '@Hooks/useConfig'
import { usePageTitle } from '@Hooks/usePageTitle'
import classes from '@Styles/About.module.css'
import logoClasses from '@Styles/LogoHeader.module.css'

const About: FC = () => {
  const { config } = useConfig()
  const { repo, valid, rawTag: tag, sha, buildTime } = ValidatedRepoMeta()
  const { t } = useTranslation()
  const theme = useMantineTheme()
  const shortSha = `#${sha.substring(0, 8)}`

  usePageTitle(t('common.title.about'))

  return (
    <WithNavBar>
      <Stack justify="center" align="center" h="calc(100vh - 16px)" gap="xl" p="xl">
        <Center>
          <Stack align="center" gap="sm">
            <MainIcon size="5rem" />
            <Title order={1} size="3.5rem" fw={800} ta="center">
              GZ<span className={logoClasses.brand}>::</span>CTF
            </Title>
            <Text size="xl" fw={500} ta="center" c="dimmed" ff="monospace" mt="xs">
              &gt;&nbsp;{t('common.content.about.slogan')}
              <Text span className={classes.blink}>
                _
              </Text>
            </Text>
          </Stack>
        </Center>

        <Flex gap="xl" wrap="wrap" justify="center" align="center">
          <Stack align="center" gap="md" miw="300px">
            <Group gap="xs" justify="center">
              <Icon path={mdiAccountGroup} size={1} />
              <Title order={3} fw={600} ta="center">
                {t('common.content.about.contributors')}
              </Title>
            </Group>
            <Group gap="sm" wrap="wrap" justify="center">
              {contributorsData.map((contributor) => (
                <Anchor
                  key={contributor.login}
                  href={contributor.html_url}
                  c={theme.primaryColor}
                  size="md"
                  fw={500}
                  underline="hover"
                >
                  @{contributor.login}
                </Anchor>
              ))}
            </Group>
          </Stack>

          <Stack align="center" gap="md" miw="300px">
            <Group gap="xs" justify="center">
              <Icon path={mdiLink} size={1} />
              <Title order={3} fw={600} ta="center">
                {t('common.content.about.resources')}
              </Title>
            </Group>
            <Stack gap="md" align="center">
              <Group gap="sm" justify="center" align="center">
                <Icon path={mdiFileDocumentOutline} size={0.8} />
                <Anchor href="https://gzctf.gzti.me" c={theme.primaryColor} size="md" fw={500} underline="hover">
                  {t('common.content.about.documentation')}
                </Anchor>
              </Group>
              <Group gap="sm" justify="center" align="center">
                <Icon path={mdiGithub} size={0.8} />
                <Anchor href={repo} c={theme.primaryColor} size="md" fw={500} underline="hover">
                  {t('common.content.about.repository')}
                </Anchor>
              </Group>
              <Group gap="sm" justify="center" align="center">
                <Icon path={mdiScaleBalance} size={0.8} />
                <Text size="sm" fw={400} c="dimmed">
                  Licensed&nbsp;under&nbsp;
                  <Anchor
                    href="https://www.gnu.org/licenses/agpl-3.0.html"
                    c={theme.primaryColor}
                    size="md"
                    fw={500}
                    underline="hover"
                  >
                    AGPLv3.0
                  </Anchor>
                </Text>
              </Group>
            </Stack>
          </Stack>
        </Flex>

        <Stack align="center" gap="md">
          <Group gap="xs" justify="center">
            <Icon path={mdiTag} size={1} />
            <Title order={3} fw={600} ta="center">
              {t('common.content.about.version')}
            </Title>
          </Group>
          <Flex direction="column" align="center" gap="sm">
            <Badge size="lg" variant="dot" color={valid ? 'green' : 'red'}>
              {valid ? `${tag}${shortSha}` : 'UNOFFICIAL'}
            </Badge>
            <Text size="xs" fw={400} c="gray" ta="center" ff="monospace">
              {valid ? `Built at ${buildTime.format('YYYY-MM-DDTHH:mm:ssZ')}` : 'This release is not officially built'}
            </Text>
          </Flex>
        </Stack>

        <Center>
          <Text size="sm" fw={400} c="dimmed" ta="center">
            Copyright&nbsp;©&nbsp;2022-now&nbsp;
            <Anchor href="https://github.com/GZTimeWalker" c="dimmed" size="sm" fw={500} underline="hover">
              @GZTimeWalker
            </Anchor>
            ,&nbsp;All&nbsp;Rights&nbsp;Reserved.
          </Text>
        </Center>
      </Stack>
    </WithNavBar>
  )
}

export default About
