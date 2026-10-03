import { Anchor, Avatar, Box, Group, SimpleGrid, Stack, Text, Title } from '@mantine/core'
import { mdiAccountGroup, mdiFileDocumentOutline, mdiGithub, mdiLink, mdiScaleBalance } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import contributorsData from 'virtual:contributors'
import { WithNavBar } from '@Components/WithNavbar'
import { MainIcon } from '@Components/icon/MainIcon'
import { useIsMobile } from '@Utils/ThemeOverride'
import { ValidatedRepoMeta } from '@Hooks/useConfig'
import { usePageTitle } from '@Hooks/usePageTitle'
import classes from '@Styles/About.module.css'
import logoClasses from '@Styles/LogoHeader.module.css'

interface ResourceLinkProps {
  icon: string
  href: string
  children: ReactNode
  prefix?: ReactNode
}

const ResourceLink: FC<ResourceLinkProps> = ({ icon, href, children, prefix }) => (
  <Anchor href={href} target="_blank" underline="never" className={classes.resourceLink}>
    <Icon path={icon} size={0.9} className={classes.resourceIcon} />
    <Text span size="sm" fw={500} className={classes.resourceText}>
      {prefix}
      {children}
    </Text>
  </Anchor>
)

const About: FC = () => {
  const { repo, valid, rawTag: tag, sha, buildTime } = ValidatedRepoMeta()
  const { t } = useTranslation()
  const shortSha = sha.substring(0, 8)

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
      <Box className={classes.root} data-mobile={isMobile || undefined}>
        <Box className={classes.backdrop} aria-hidden />
        <Stack align="center" gap="xl" className={classes.container}>
          {/* Hero */}
          <Stack align="center" gap={0} className={classes.hero}>
            <Box className={classes.iconGlow}>
              <MainIcon size="5rem" className={classes.mainIcon} />
            </Box>
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

          {/* Resources & Version */}
          <SimpleGrid cols={{ base: 1, md: 2 }} spacing="lg" className={classes.cardGrid}>
            <Box className={classes.card}>
              <Group gap="xs" className={classes.cardHeader}>
                <Icon path={mdiLink} size={0.9} />
                <Title order={4} fw={600}>
                  {t('common.content.about.resources')}
                </Title>
              </Group>
              <Stack gap={4} className={classes.cardBody}>
                <ResourceLink icon={mdiFileDocumentOutline} href="https://gzctf.gzti.me">
                  {t('common.content.about.documentation')}
                </ResourceLink>
                <ResourceLink icon={mdiGithub} href={repo}>
                  {t('common.content.about.repository')}
                </ResourceLink>
                <ResourceLink
                  icon={mdiScaleBalance}
                  href="https://www.gnu.org/licenses/agpl-3.0.html"
                  prefix={<span className={classes.resourcePrefix}>Licensed under&nbsp;</span>}
                >
                  AGPLv3.0
                </ResourceLink>
                <ResourceLink
                  icon={mdiScaleBalance}
                  href="https://github.com/GZTimeWalker/GZCTF/blob/develop/license/LicenseRef-GZCTF-Restricted.txt"
                  prefix={<span className={classes.resourcePrefix}>Licensed under&nbsp;</span>}
                >
                  LicenseRef-GZCTF-Restricted
                </ResourceLink>
              </Stack>
            </Box>

            <Box className={classes.terminal}>
              <Box className={classes.termHeader}>
                <span className={classes.dot} data-c="r" />
                <span className={classes.dot} data-c="y" />
                <span className={classes.dot} data-c="g" />
                <Text span size="xs" ff="monospace" c="dimmed" className={classes.termTitle}>
                  gzctf@about:~
                </Text>
              </Box>
              <Box className={classes.termBody}>
                <div>
                  <span className={classes.prompt}>$</span> gzctf --version
                </div>
                {valid ? (
                  <>
                    <div>
                      <span className={classes.termKey}>tag</span>
                      {tag}
                    </div>
                    <div>
                      <span className={classes.termKey}>commit</span>
                      {shortSha}
                    </div>
                    <div>
                      <span className={classes.termKey}>built</span>
                      {buildTime.format('YYYY-MM-DDTHH:mm:ssZ')}
                    </div>
                    <div>
                      <span className={classes.termKey}>status</span>
                      <span className={classes.termStatus} data-valid>
                        ● official build
                      </span>
                    </div>
                  </>
                ) : (
                  <>
                    <div>
                      <span className={classes.termKey}>status</span>
                      <span className={classes.termStatus}>● unofficial build</span>
                    </div>
                    <div className={classes.termNote}>This release is not officially built</div>
                  </>
                )}
                <div>
                  <span className={classes.prompt}>$</span>
                  <span className={classes.blink}>&nbsp;_</span>
                </div>
              </Box>
            </Box>
          </SimpleGrid>

          {/* Contributors */}
          <Box className={classes.card} w="100%">
            <Group gap="xs" className={classes.cardHeader}>
              <Icon path={mdiAccountGroup} size={0.9} />
              <Title order={4} fw={600}>
                {t('common.content.about.contributors')}
              </Title>
            </Group>
            <Stack gap="xs" className={classes.cardBody}>
              {groups.map((group, index) => (
                <div key={index} className={classes.scrollContainer}>
                  <div className={classes.scrollGroup} data-reverse={index % 2 === 1 || undefined}>
                    {group.concat(group, group).map((contributor, i) => (
                      <Anchor
                        key={`${contributor.login}-${i}`}
                        href={contributor.html_url}
                        target="_blank"
                        underline="never"
                        className={classes.chip}
                      >
                        <Avatar src={`https://github.com/${contributor.login}.png`} size={24} />
                        <Text span size="sm" fw={500} className={classes.chipText}>
                          @{contributor.login}
                        </Text>
                      </Anchor>
                    ))}
                  </div>
                </div>
              ))}
            </Stack>
          </Box>

          {/* Footer */}
          <Text size="sm" fw={400} c="dimmed" ta="center" className={classes.copyright}>
            Copyright&nbsp;©&nbsp;
            <span style={{ whiteSpace: 'nowrap' }}>2022-now</span>
            &nbsp;
            <Anchor href="https://github.com/GZTimeWalker" target="_blank" c="dimmed" size="sm" fw={500}>
              @GZTimeWalker
            </Anchor>
          </Text>
        </Stack>
      </Box>
    </WithNavBar>
  )
}

export default About
