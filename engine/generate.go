package engine

import (
	"context"
	"fmt"
	"log"
	"net/http"
	"net/http/httptest"

	"github.com/chromedp/cdproto/page"
	"github.com/chromedp/chromedp"
)

func GeneratePDF(htmlContent string) ([]byte, error) {

	ts := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "text/html; charset=utf-8")
		fmt.Fprint(w, htmlContent)
	}))
	defer ts.Close()

	opts := append(chromedp.DefaultExecAllocatorOptions[:],
		chromedp.NoSandbox,
		chromedp.Headless,
		chromedp.DisableGPU,
		chromedp.NoFirstRun,
		chromedp.NoDefaultBrowserCheck,
	)

	allocCtx, cancel := chromedp.NewExecAllocator(context.Background(), opts...)
	defer cancel()


	ctx, cancel := chromedp.NewContext(allocCtx, chromedp.WithLogf(log.Printf))
	defer cancel()


	var pdfBuffer []byte

	err := chromedp.Run(ctx,
		chromedp.Navigate(ts.URL),
		chromedp.WaitReady("body"),
		chromedp.ActionFunc(func(ctx context.Context) error {
			var err error
			pdfBuffer, _, err = page.PrintToPDF().
				WithPrintBackground(true).
				WithPaperWidth(8.27).          
				WithPaperHeight(11.69).         
				WithMarginTop(0.39).            
				WithMarginBottom(0.39).         
				WithMarginLeft(0.39).          
				WithMarginRight(0.39).         
				WithDisplayHeaderFooter(false). 
				WithPreferCSSPageSize(false).   
				Do(ctx)
			return err
		}),
	)

	if err != nil {
		return nil, fmt.Errorf("chromedp ile PDF oluşturma başarısız: %w", err)
	}

	return pdfBuffer, nil
}

func GeneratePDFWithOptions(htmlContent string, opts PDFOptions) ([]byte, error) {
	ts := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "text/html; charset=utf-8")
		fmt.Fprint(w, htmlContent)
	}))
	defer ts.Close()

	allocOpts := append(chromedp.DefaultExecAllocatorOptions[:],
		chromedp.NoSandbox,
		chromedp.Headless,
		chromedp.DisableGPU,
	)

	allocCtx, cancel := chromedp.NewExecAllocator(context.Background(), allocOpts...)
	defer cancel()

	ctx, cancel := chromedp.NewContext(allocCtx)
	defer cancel()

	var pdfBuffer []byte

	err := chromedp.Run(ctx,
		chromedp.Navigate(ts.URL),
		chromedp.WaitReady("body"),
		chromedp.ActionFunc(func(ctx context.Context) error {
			printer := page.PrintToPDF().
				WithPrintBackground(opts.PrintBackground).
				WithPaperWidth(opts.PaperWidth).
				WithPaperHeight(opts.PaperHeight).
				WithMarginTop(opts.MarginTop).
				WithMarginBottom(opts.MarginBottom).
				WithMarginLeft(opts.MarginLeft).
				WithMarginRight(opts.MarginRight).
				WithDisplayHeaderFooter(opts.DisplayHeaderFooter).
				WithPreferCSSPageSize(opts.PreferCSSPageSize)

			if opts.Landscape {
				printer = printer.WithLandscape(true)
			}

			if opts.HeaderTemplate != "" {
				printer = printer.WithHeaderTemplate(opts.HeaderTemplate)
			}

			if opts.FooterTemplate != "" {
				printer = printer.WithFooterTemplate(opts.FooterTemplate)
			}

			var err error
			pdfBuffer, _, err = printer.Do(ctx)
			return err
		}),
	)

	if err != nil {
		return nil, fmt.Errorf("chromedp ile PDF oluşturma başarısız: %w", err)
	}

	return pdfBuffer, nil
}


type PDFOptions struct {
	// Paper dimensions (in inches)
	PaperWidth  float64
	PaperHeight float64

	// Margins (in inches)
	MarginTop    float64
	MarginBottom float64
	MarginLeft   float64
	MarginRight  float64

	PrintBackground     bool
	Landscape           bool
	DisplayHeaderFooter bool
	PreferCSSPageSize   bool
	HeaderTemplate string
	FooterTemplate string
}


func DefaultPDFOptions() PDFOptions {
	return PDFOptions{
		PaperWidth:          8.27,  // A4
		PaperHeight:         11.69, // A4
		MarginTop:           0.39,  // ~10mm
		MarginBottom:        0.39,
		MarginLeft:          0.39,
		MarginRight:         0.39,
		PrintBackground:     true,
		Landscape:           false,
		DisplayHeaderFooter: false,
		PreferCSSPageSize:   false,
	}
}


func LetterPDFOptions() PDFOptions {
	return PDFOptions{
		PaperWidth:          8.5,
		PaperHeight:         11,
		MarginTop:           0.39,
		MarginBottom:        0.39,
		MarginLeft:          0.39,
		MarginRight:         0.39,
		PrintBackground:     true,
		Landscape:           false,
		DisplayHeaderFooter: false,
		PreferCSSPageSize:   false,
	}
}