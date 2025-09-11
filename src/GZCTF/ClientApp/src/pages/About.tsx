import {
  Anchor,
  Center,
  Group,
  Stack,
  Text,
  Title,
  useMantineTheme,
  Flex,
  Badge,
  Avatar,
  Container,
} from '@mantine/core'
import { mdiScaleBalance, mdiFileDocumentOutline, mdiGithub, mdiTag, mdiAccountGroup, mdiLink } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC } from 'react'
import { useTranslation } from 'react-i18next'
import contributorsData from 'virtual:contributors'
import { WithNavBar } from '@Components/WithNavbar'
import { MainIcon } from '@Components/icon/MainIcon'
import { useIsMobile } from '@Utils/ThemeOverride'
import { ValidatedRepoMeta } from '@Hooks/useConfig'
import { usePageTitle } from '@Hooks/usePageTitle'
import classes from '@Styles/About.module.css'
import logoClasses from '@Styles/LogoHeader.module.css'

const About: FC = () => {
  const { repo, valid, rawTag: tag, sha, buildTime } = ValidatedRepoMeta()
  const { t } = useTranslation()
  const theme = useMantineTheme()
  const shortSha = `#${sha.substring(0, 8)}`

  const isMobile = useIsMobile()

  const numRows = isMobile ? 4 : 3
  const groups = Array.from({ length: numRows }, (_, i) =>
    contributorsData.slice(
      i * Math.ceil(contributorsData.length / numRows),
      (i + 1) * Math.ceil(contributorsData.length / numRows)
    )
  )

  usePageTitle(t('common.title.about'))

  return (
    <WithNavBar minWidth={0}>
      <Stack justify="center" align="center" gap="xl" className={classes.container} data-mobile={isMobile || undefined}>
        <Center>
          <Stack align="center" gap={0}>
            <MainIcon size="5rem" className={classes.mainIcon} />
            <Title order={1} size="3.5rem" fw={800} ta="center" className={classes.mainTitle}>
              GZ<span className={logoClasses.brand}>::</span>CTF
            </Title>
            <Text size="xl" fw={500} ta="center" c="dimmed" ff="monospace" mt="xs" className={classes.slogan}>
              &gt;&nbsp;{t('common.content.about.slogan')}
              <Text span className={classes.blink}>
                _
              </Text>
            </Text>
          </Stack>
        </Center>

        <Flex
          gap={isMobile ? 'md' : 'xl'}
          direction="column"
          wrap="wrap"
          justify="center"
          align="center"
          className={classes.contentFlex}
        >
          <Stack align="center" gap="md" className={classes.contentStack}>
            <Group gap="xs" justify="center">
              <Icon path={mdiLink} size={1} />
              <Title order={3} fw={600} ta="center">
                {t('common.content.about.resources')}
              </Title>
            </Group>
            <Stack gap="xs" align="center">
              <Group gap="sm" justify="center" align="center">
                <Icon path={mdiFileDocumentOutline} size={0.8} />
                <Anchor
                  href="https://gzctf.gzti.me"
                  target="_blank"
                  c={theme.primaryColor}
                  size="md"
                  fw={500}
                  underline="hover"
                  className={classes.resourceLink}
                >
                  {t('common.content.about.documentation')}
                </Anchor>
              </Group>
              <Group gap="sm" justify="center" align="center">
                <Icon path={mdiGithub} size={0.8} />
                <Anchor
                  href={repo}
                  target="_blank"
                  c={theme.primaryColor}
                  size="md"
                  fw={500}
                  underline="hover"
                  className={classes.resourceLink}
                >
                  {t('common.content.about.repository')}
                </Anchor>
              </Group>
              <Group gap="sm" justify="center" align="center">
                <Icon path={mdiScaleBalance} size={0.8} />
                <Text size="sm" fw={400} c="dimmed" ta="center" className={classes.licenseText}>
                  Licensed&nbsp;under&nbsp;
                  <Anchor
                    href="https://www.gnu.org/licenses/agpl-3.0.html"
                    target="_blank"
                    c={theme.primaryColor}
                    size="md"
                    fw={500}
                    underline="hover"
                    className={classes.licenseLink}
                  >
                    AGPLv3.0
                  </Anchor>
                </Text>
              </Group>
              <Group gap="sm" justify="center" align="center">
                <Icon path={mdiScaleBalance} size={0.8} />
                <Text size="sm" fw={400} c="dimmed" ta="center" className={classes.licenseText}>
                  Licensed&nbsp;under&nbsp;
                  <Anchor
                    href="https://github.com/GZTimeWalker/GZCTF/blob/develop/license/LicenseRef-GZCTF-Restricted.txt"
                    target="_blank"
                    c={theme.primaryColor}
                    size="sm"
                    fw={500}
                    underline="hover"
                    className={classes.resourceLink}
                  >
                    LicenseRef-GZCTF-Restricted
                  </Anchor>
                </Text>
              </Group>
            </Stack>
          </Stack>

          <Stack align="center" gap="md" className={classes.contentStack}>
            <Group gap="xs" justify="center">
              <Icon path={mdiAccountGroup} size={1} />
              <Title order={3} fw={600} ta="center">
                {t('common.content.about.contributors')}
              </Title>
            </Group>
            <Container size="md" px={0}>
              <Stack gap={0} align="center">
                {groups.map((group, index) => (
                  <div key={index} className={classes.scrollContainer}>
                    <Group gap={0} wrap="nowrap" w="max-content" className={classes.scrollGroup}>
                      {group.concat(group, group).map((contributor, i) => (
                        <Group key={`${contributor.login}-${i}`} gap={2} align="center" justify="center" mr="md">
                          <Avatar
                            className={classes.contributorAvatar}
                            src={`https://github.com/${contributor.login}.png`}
                            size="sm"
                          />
                          <Anchor
                            href={contributor.html_url}
                            target="_blank"
                            c={theme.primaryColor}
                            size="sm"
                            fw={500}
                            underline="hover"
                            className={classes.contributorLink}
                          >
                            @{contributor.login}
                          </Anchor>
                        </Group>
                      ))}
                    </Group>
                  </div>
                ))}
              </Stack>
            </Container>
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
            <Badge size="lg" variant="dot" color={valid ? 'green' : 'red'} className={classes.versionBadge}>
              {valid ? `${tag}${shortSha}` : 'UNOFFICIAL'}
            </Badge>
            <Text size="xs" fw={400} c="gray" ta="center" ff="monospace">
              {valid ? `Built at ${buildTime.format('YYYY-MM-DDTHH:mm:ssZ')}` : 'This release is not officially built'}
            </Text>
          </Flex>
        </Stack>

        <Center>
          <Text size="sm" fw={400} c="dimmed" ta="center" maw="100%" className={classes.copyright}>
            Copyright&nbsp;©&nbsp;
            <span style={{ whiteSpace: 'nowrap' }}>2022-now</span>
            &nbsp;
            <Anchor
              href="https://github.com/GZTimeWalker"
              target="_blank"
              c="dimmed"
              size="sm"
              fw={500}
              underline="hover"
            >
              @GZTimeWalker
            </Anchor>
          </Text>
        </Center>
      </Stack>
    </WithNavBar>
  )
}

export default About
